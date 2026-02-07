using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace MagicSorter.Tests
{
    /// <summary>
    /// Tests for item grouping and consolidation behavior.
    /// Uses mock data structures since actual game classes aren't available in test context.
    /// </summary>
    [TestFixture]
    public class ItemGroupingTests
    {
        #region Mock Classes

        private class MockItemStack
        {
            public int ItemType { get; set; }
            public string ItemName { get; set; }
            public int Count { get; set; }

            public MockItemStack(int type, string name, int count = 1)
            {
                ItemType = type;
                ItemName = name;
                Count = count;
            }
        }

        private class MockContainer
        {
            public string Name { get; set; }
            public string Category { get; set; }
            public int Capacity { get; set; }
            public List<MockItemStack> Items { get; set; } = new List<MockItemStack>();

            public MockContainer(string name, string category, int capacity = 45)
            {
                Name = name;
                Category = category;
                Capacity = capacity;
            }

            public int CountItemType(int itemType)
            {
                return Items.Count(i => i.ItemType == itemType);
            }

            public bool HasSpace => Items.Count < Capacity;

            public float Fullness => Items.Count / (float)Capacity;
        }

        #endregion

        #region Grouping By Type Tests

        [Test]
        public void GroupingByType_ItemsOfSameTypeProcessedTogether()
        {
            // Arrange: Mixed items in random order
            var items = new List<MockItemStack>
            {
                new MockItemStack(1, "Engine", 5),
                new MockItemStack(2, "Sand", 10),
                new MockItemStack(1, "Engine", 3),
                new MockItemStack(3, "Iron", 20),
                new MockItemStack(2, "Sand", 15),
                new MockItemStack(1, "Engine", 2)
            };

            // Act: Group by item type (simulates our sorting logic)
            var sortedByType = items.OrderBy(i => i.ItemType).ToList();

            // Assert: Items of same type are consecutive
            var previousType = -1;
            var seenTypes = new HashSet<int>();
            foreach (var item in sortedByType)
            {
                if (item.ItemType != previousType)
                {
                    // When we see a new type, we shouldn't have seen it before
                    Assert.IsFalse(seenTypes.Contains(item.ItemType),
                        $"Item type {item.ItemType} appeared non-consecutively");
                    seenTypes.Add(item.ItemType);
                    previousType = item.ItemType;
                }
            }
        }

        [Test]
        public void GroupingByType_AllEnginesTogether()
        {
            var items = new List<MockItemStack>
            {
                new MockItemStack(1, "Engine", 1),
                new MockItemStack(2, "Sand", 1),
                new MockItemStack(1, "Engine", 1),
                new MockItemStack(2, "Sand", 1),
                new MockItemStack(1, "Engine", 1)
            };

            var sortedByType = items.OrderBy(i => i.ItemType).ToList();

            // First 3 should be engines (type 1)
            Assert.AreEqual(1, sortedByType[0].ItemType);
            Assert.AreEqual(1, sortedByType[1].ItemType);
            Assert.AreEqual(1, sortedByType[2].ItemType);

            // Last 2 should be sand (type 2)
            Assert.AreEqual(2, sortedByType[3].ItemType);
            Assert.AreEqual(2, sortedByType[4].ItemType);
        }

        #endregion

        #region Container Selection Tests

        [Test]
        public void ContainerSelection_PrefersContainerWithSameItemType()
        {
            // Arrange: Two containers, one already has engines
            var containerA = new MockContainer("Resources A", "resources");
            containerA.Items.Add(new MockItemStack(1, "Engine", 5));
            containerA.Items.Add(new MockItemStack(1, "Engine", 3));

            var containerB = new MockContainer("Resources B", "resources");
            containerB.Items.Add(new MockItemStack(2, "Sand", 10));
            containerB.Items.Add(new MockItemStack(3, "Iron", 20));

            var containers = new List<MockContainer> { containerA, containerB };

            // Act: Find best container for an engine (type 1)
            var engineType = 1;
            var bestContainer = containers
                .Where(c => c.HasSpace)
                .OrderByDescending(c => c.CountItemType(engineType))
                .ThenByDescending(c => c.Fullness)
                .FirstOrDefault();

            // Assert: Should prefer container A (has engines)
            Assert.AreEqual(containerA, bestContainer);
        }

        [Test]
        public void ContainerSelection_FallsBackToFullnessWhenNoMatchingItems()
        {
            // Arrange: Two empty containers with different fullness
            var containerA = new MockContainer("Resources A", "resources");
            // Add some items to make it fuller
            for (int i = 0; i < 10; i++)
                containerA.Items.Add(new MockItemStack(99, "Other"));

            var containerB = new MockContainer("Resources B", "resources");
            // Empty

            var containers = new List<MockContainer> { containerA, containerB };

            // Act: Find best container for a new item type (type 50)
            var newItemType = 50;
            var bestContainer = containers
                .Where(c => c.HasSpace)
                .OrderByDescending(c => c.CountItemType(newItemType))
                .ThenByDescending(c => c.Fullness)
                .FirstOrDefault();

            // Assert: Should prefer container A (fuller) since neither has the item type
            Assert.AreEqual(containerA, bestContainer);
        }

        [Test]
        public void ContainerSelection_SkipsFullContainers()
        {
            // Arrange: One full container with matching items, one with space
            var containerA = new MockContainer("Resources A", "resources", capacity: 2);
            containerA.Items.Add(new MockItemStack(1, "Engine"));
            containerA.Items.Add(new MockItemStack(1, "Engine"));
            // Container A is now full

            var containerB = new MockContainer("Resources B", "resources", capacity: 10);
            // Empty, has space

            var containers = new List<MockContainer> { containerA, containerB };

            // Act: Find best container for an engine
            var engineType = 1;
            var bestContainer = containers
                .Where(c => c.HasSpace)
                .OrderByDescending(c => c.CountItemType(engineType))
                .ThenByDescending(c => c.Fullness)
                .FirstOrDefault();

            // Assert: Should pick container B (only one with space)
            Assert.AreEqual(containerB, bestContainer);
        }

        #endregion

        #region Consolidation Scenario Tests

        [Test]
        public void Consolidation_ScatteredEnginesEndUpTogether()
        {
            // Arrange: Simulate the consolidation scenario
            // Container A has 2 engines and 5 sand
            // Container B has 3 engines and 3 iron
            // We want all engines to end up in one container

            var allItems = new List<MockItemStack>
            {
                // From Container A
                new MockItemStack(1, "Engine", 2),
                new MockItemStack(2, "Sand", 5),
                // From Container B
                new MockItemStack(1, "Engine", 3),
                new MockItemStack(3, "Iron", 3)
            };

            // Empty containers for re-sorting
            var containerA = new MockContainer("Resources A", "resources", 45);
            var containerB = new MockContainer("Resources B", "resources", 45);
            var containers = new List<MockContainer> { containerA, containerB };

            // Act: Sort items by type and place them
            var sortedByType = allItems.OrderBy(i => i.ItemType).ToList();

            foreach (var item in sortedByType)
            {
                // Find best container (prefers same item type, then fullness)
                var bestContainer = containers
                    .Where(c => c.HasSpace)
                    .OrderByDescending(c => c.CountItemType(item.ItemType))
                    .ThenByDescending(c => c.Fullness)
                    .First();

                bestContainer.Items.Add(item);
            }

            // Assert: All engines should be in one container
            var containersWithEngines = containers.Where(c => c.CountItemType(1) > 0).ToList();
            Assert.AreEqual(1, containersWithEngines.Count,
                "All engines should be consolidated into one container");

            // And that container should have all 5 engine stacks worth
            var engineContainer = containersWithEngines.First();
            Assert.AreEqual(2, engineContainer.CountItemType(1),
                "Engine container should have both engine stacks");
        }

        [Test]
        public void Consolidation_NewItemGoesToContainerWithExisting()
        {
            // Arrange: Container A already has sand, Container B is empty
            var containerA = new MockContainer("Resources A", "resources", 45);
            containerA.Items.Add(new MockItemStack(2, "Sand", 10));

            var containerB = new MockContainer("Resources B", "resources", 45);

            var containers = new List<MockContainer> { containerA, containerB };

            // Act: Add new sand
            var newSand = new MockItemStack(2, "Sand", 5);
            var bestContainer = containers
                .Where(c => c.HasSpace)
                .OrderByDescending(c => c.CountItemType(newSand.ItemType))
                .ThenByDescending(c => c.Fullness)
                .First();

            bestContainer.Items.Add(newSand);

            // Assert: New sand went to container A (which already had sand)
            Assert.AreEqual(2, containerA.CountItemType(2), "Sand should be consolidated in container A");
            Assert.AreEqual(0, containerB.CountItemType(2), "Container B should have no sand");
        }

        #endregion

        #region Edge Cases

        [Test]
        public void EdgeCase_AllContainersFull_NoPlacement()
        {
            var containerA = new MockContainer("Resources A", "resources", capacity: 1);
            containerA.Items.Add(new MockItemStack(1, "Engine"));

            var containerB = new MockContainer("Resources B", "resources", capacity: 1);
            containerB.Items.Add(new MockItemStack(2, "Sand"));

            var containers = new List<MockContainer> { containerA, containerB };

            // Act: Try to find container for new item
            var bestContainer = containers
                .Where(c => c.HasSpace)
                .OrderByDescending(c => c.CountItemType(3))
                .ThenByDescending(c => c.Fullness)
                .FirstOrDefault();

            // Assert: No container available
            Assert.IsNull(bestContainer);
        }

        [Test]
        public void EdgeCase_SingleContainer_AllItemsGoThere()
        {
            var container = new MockContainer("Resources", "resources", 100);
            var containers = new List<MockContainer> { container };

            var items = new List<MockItemStack>
            {
                new MockItemStack(1, "Engine"),
                new MockItemStack(2, "Sand"),
                new MockItemStack(3, "Iron")
            };

            // Act
            foreach (var item in items.OrderBy(i => i.ItemType))
            {
                var best = containers.Where(c => c.HasSpace).FirstOrDefault();
                if (best != null) best.Items.Add(item);
            }

            // Assert: All items in the single container
            Assert.AreEqual(3, container.Items.Count);
        }

        [Test]
        public void EdgeCase_EmptyItemList_NothingHappens()
        {
            var container = new MockContainer("Resources", "resources");
            var items = new List<MockItemStack>();

            var sortedByType = items.OrderBy(i => i.ItemType).ToList();

            Assert.AreEqual(0, sortedByType.Count);
            Assert.AreEqual(0, container.Items.Count);
        }

        #endregion
    }
}
