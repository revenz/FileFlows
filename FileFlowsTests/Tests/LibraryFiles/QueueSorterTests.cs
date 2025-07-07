using FileFlows.Services.FileProcessing;

namespace FileFlowsTests.Tests.LibraryFiles;

/// <summary>
/// Unit tests for <see cref="QueueSortHelper"/> sorting logic, including complex scenarios.
/// </summary>
[TestClass]
public class QueueSorterTests
{
    private Guid libA = Guid.NewGuid();
    private Guid libB = Guid.NewGuid();
    private Guid libHighRandom = Guid.NewGuid();
    private Guid libNormalAlpha = Guid.NewGuid();
    private Guid libLowOldest = Guid.NewGuid();
    
    /// <summary>
    /// Creates a new <see cref="LibraryFile"/> instance with specified properties for testing.
    /// </summary>
    /// <param name="name">The name of the file.</param>
    /// <param name="libUid">The library UID the file belongs to, or null if none.</param>
    /// <param name="order">Explicit order value; files with order &gt; 0 are prioritized in sorting.</param>
    /// <param name="size">The original size of the file in bytes.</param>
    /// <param name="creation">The creation date/time of the file. If null, defaults to <see cref="DateTime.Now"/>.</param>
    /// <param name="reprocessing">Flag indicating if the file is marked for reprocessing.</param>
    /// <returns>A new <see cref="LibraryFile"/> instance with the given properties set.</returns>
    private LibraryFile CreateFile(string name, Guid? libUid, int order = 0, long size = 1000,
        DateTime? creation = null, bool reprocessing = false)
    {
        return new LibraryFile
        {
            Uid = Guid.NewGuid(),
            Name = name,
            LibraryUid = libUid,
            Order = order,
            OriginalSize = size,
            CreationTime = creation ?? DateTime.Now,
            DateCreated = creation ?? DateTime.Now,
            DateModified = (creation ?? DateTime.Now).AddHours(1),
            Additional = new() { Reprocessing = reprocessing }
        };
    }

    /// <summary>
    /// Returns a sample dictionary mapping library UIDs to their priority and processing order,
    /// used for configuring sorting behavior in tests.
    /// </summary>
    /// <returns>A dictionary where keys are library UIDs and values are tuples containing <see cref="ProcessingPriority"/> and <see cref="ProcessingOrder"/>.</returns>
    private Dictionary<Guid, (ProcessingPriority Priority, ProcessingOrder Order)> GetComplexLibInfos()
    {
        return new Dictionary<Guid, (ProcessingPriority, ProcessingOrder)>
        {
            { libHighRandom, (ProcessingPriority.High, ProcessingOrder.Random) },
            { libNormalAlpha, (ProcessingPriority.Normal, ProcessingOrder.Alphabetical) },
            { libLowOldest, (ProcessingPriority.Low, ProcessingOrder.OldestFirst) }
        };
    }

    private Dictionary<Guid, (ProcessingPriority Priority, ProcessingOrder Order)> GetLibInfos()
    {
        return new Dictionary<Guid, (ProcessingPriority, ProcessingOrder)>
        {
            { libA, (ProcessingPriority.High, ProcessingOrder.LargestFirst) },
            { libB, (ProcessingPriority.Normal, ProcessingOrder.OldestFirst) }
        };
    }

    /// <summary>
    /// Tests that files with explicit <c>Order</c> values greater than zero
    /// are sorted first in ascending order, regardless of other sorting criteria.
    /// </summary>
    [TestMethod]
    public void SortFiles_OrdersFirst()
    {
        var files = new List<LibraryFile>
        {
            CreateFile("F1", libA, order: 2),
            CreateFile("F2", libA, order: 1),
            CreateFile("F3", libA),
            CreateFile("F4", libB)
        };

        var sorted = QueueSortHelper.SortFiles(files, GetLibInfos(), advancedProcessing: false);

        // Files with Order > 0 come first and sorted by Order ascending
        Assert.AreEqual(files[1].Uid, sorted[0].Uid); // F2 order=1
        Assert.AreEqual(files[0].Uid, sorted[1].Uid); // F1 order=2
        // Then the others sorted by DateCreated (default)
        Assert.IsTrue(sorted.Skip(2).Any(f => f.Name == "F3"));
        Assert.IsTrue(sorted.Skip(2).Any(f => f.Name == "F4"));
    }

    /// <summary>
    /// Tests simple sorting mode (<c>advancedProcessing = false</c>), where
    /// files without explicit order are sorted by <see cref="LibraryFile.DateCreated"/> ascending.
    /// </summary>
    [TestMethod]
    public void SortFiles_SimpleProcessing()
    {
        var creation1 = new DateTime(2023, 1, 1);
        var creation2 = new DateTime(2024, 1, 1);

        var files = new List<LibraryFile>
        {
            CreateFile("F1", libA, creation: creation2),
            CreateFile("F2", libB, creation: creation1)
        };

        var sorted = QueueSortHelper.SortFiles(files, GetLibInfos(), advancedProcessing: false);

        // Should sort by DateCreated ascending
        Assert.AreEqual("F2", sorted[0].Name);
        Assert.AreEqual("F1", sorted[1].Name);
    }


    /// <summary>
    /// Tests advanced processing sorting mode (<c>advancedProcessing = true</c>),
    /// verifying grouping by library priority and sorting inside each library according to
    /// its configured <see cref="ProcessingOrder"/>.
    /// </summary>
    [TestMethod]
    public void SortFiles_AdvancedProcessing_PriorityAndLibraryOrder()
    {
        var creationOld = new DateTime(2023, 1, 1);
        var creationNew = new DateTime(2024, 1, 1);

        var files = new List<LibraryFile>
        {
            CreateFile("A1", libA, size: 300, creation: creationNew),
            CreateFile("A2", libA, size: 1000, creation: creationOld),
            CreateFile("B1", libB, size: 500, creation: creationOld),
            CreateFile("B2", libB, size: 800, creation: creationNew),
        };

        var sorted = QueueSortHelper.SortFiles(files, GetLibInfos(), advancedProcessing: true);

        // libA has higher priority than libB, so all libA files come first
        var libAFiles = sorted.Take(2).ToList();
        var libBFiles = sorted.Skip(2).Take(2).ToList();

        Assert.IsTrue(libAFiles.All(f => f.LibraryUid == libA));
        Assert.IsTrue(libBFiles.All(f => f.LibraryUid == libB));

        // libA files should be sorted LargestFirst (size descending)
        Assert.AreEqual("A2", libAFiles[0].Name); // 1000
        Assert.AreEqual("A1", libAFiles[1].Name); // 300

        // libB files sorted OldestFirst (creation ascending)
        Assert.AreEqual("B1", libBFiles[0].Name); // old creation
        Assert.AreEqual("B2", libBFiles[1].Name); // new creation
    }
    
    /// <summary>
    /// Tests that when multiple libraries have the same priority, files from each library
    /// are interleaved (round-robin) in the sorted output.  
    /// Also verifies that each library’s internal sort order is respected.
    /// </summary>
    [TestMethod]
    public void SortFiles_AdvancedProcessing_RoundRobinInterleave()
    {
        // Both libs have same priority, so files interleaved round robin
        var libInfos = new Dictionary<Guid, (ProcessingPriority, ProcessingOrder)>
        {
            { libA, (ProcessingPriority.High, ProcessingOrder.LargestFirst) },
            { libB, (ProcessingPriority.High, ProcessingOrder.SmallestFirst) }
        };

        var files = new List<LibraryFile>
        {
            CreateFile("A1", libA, size: 500),
            CreateFile("A2", libA, size: 1000),
            CreateFile("B1", libB, size: 300),
            CreateFile("B2", libB, size: 200),
            CreateFile("B3", libB, size: 400),
        };

        var sorted = QueueSortHelper.SortFiles(files, libInfos, advancedProcessing: true);

        // The files should be interleaved in round-robin between libraries
        // Order within libraries per their order: libA LargestFirst, libB SmallestFirst

        var expectedOrder = new[]
        {
            "A2", "B2", // first round
            "A1", "B1", // second round
            "B3" // third round (libA empty)
        };

        for (int i = 0; i < expectedOrder.Length; i++)
        {
            Assert.AreEqual(expectedOrder[i], sorted[i].Name, $"File at index {i} should be {expectedOrder[i]}");
        }
    }

    /// <summary>
    /// Tests sorting with mixed priorities, explicit order values, different library processing orders,
    /// and files flagged for reprocessing, verifying the full interleaving and ordering behavior.
    /// </summary>
    [TestMethod]
    public void SortFiles_ComplexScenario_MixedPrioritiesOrdersReprocessing()
    {
        // Arrange
        var libInfos = GetComplexLibInfos();

        var files = new List<LibraryFile>
        {
            // Explicit orders, should always come first sorted by order
            CreateFile("ExplicitOrder1", libHighRandom, order: 1),
            CreateFile("ExplicitOrder2", libNormalAlpha, order: 3),
            CreateFile("ExplicitOrder3", libLowOldest, order: 2),

            // High priority, Random order (random seeded so deterministic)
            CreateFile("RandomFile1", libHighRandom, size: 500),
            CreateFile("RandomFile2", libHighRandom, size: 1000),

            // Normal priority, Alphabetical order
            CreateFile("AlphaA", libNormalAlpha, size: 400),
            CreateFile("AlphaB", libNormalAlpha, size: 600, reprocessing: true),

            // Low priority, OldestFirst
            CreateFile("OldestNew", libLowOldest, creation: DateTime.Now.AddDays(-1)),
            CreateFile("OldestOld", libLowOldest, creation: DateTime.Now.AddDays(-10)),
            CreateFile("OldestMid", libLowOldest, creation: DateTime.Now.AddDays(-5)),

            // No library, fallback ordering by DateCreated
            CreateFile("NoLib1", null, creation: DateTime.Now.AddHours(-10)),
            CreateFile("NoLib2", null, creation: DateTime.Now.AddHours(-1)),
        };

        // Act
        var sorted = QueueSortHelper.SortFiles(files, libInfos, advancedProcessing: true);

        // Assert

        // 1) Explicit orders first by ascending order
        Assert.AreEqual("ExplicitOrder1", sorted[0].Name);
        Assert.AreEqual("ExplicitOrder3", sorted[1].Name);
        Assert.AreEqual("ExplicitOrder2", sorted[2].Name);

        // 2) Next comes High priority library files (libHighRandom), random order seeded for test consistency
        var highPriorityFiles = sorted.Skip(3).Take(2).Select(f => f.Name).ToList();
        CollectionAssert.AreEquivalent(new[] { "RandomFile1", "RandomFile2" }, highPriorityFiles);

        // 3) Then Normal priority files: interleaved from libNormalAlpha and no-library group
        var normalPriorityFiles = sorted.Skip(5).Take(4).Select(f => f.Name).ToList();
        // Because of round-robin interleaving, order isn't guaranteed, but these four must be present
        CollectionAssert.AreEquivalent(new[] { "AlphaA", "AlphaB", "NoLib1", "NoLib2" }, normalPriorityFiles);

        // 4) Then Low priority library files (libLowOldest), sorted oldest first by CreationTime
        var lowPriorityFiles = sorted.Skip(9).Take(3).Select(f => f.Name).ToList();
        CollectionAssert.AreEqual(new[] { "OldestOld", "OldestMid", "OldestNew" }, lowPriorityFiles);
    }

}