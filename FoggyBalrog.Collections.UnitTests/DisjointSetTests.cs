namespace FoggyBalrog.Collections.UnitTests;

public class DisjointSetTests
{
    // -------------------------------------------------------------------------
    // Constructor
    // -------------------------------------------------------------------------

    [Fact(DisplayName = "Constructor with empty collection creates DisjointSet with zero elements and zero sets")]
    public void Constructor_WhenEmptyCollection_ShouldCreateEmptyDisjointSet()
    {
        var ds = new DisjointSet<string>([]);

        Assert.Equal(0, ds.ElementCount);
        Assert.Equal(0, ds.SetCount);
    }

    [Fact(DisplayName = "Constructor creates one singleton set per element")]
    public void Constructor_WhenGivenElements_ShouldCreateOneSingletonSetPerElement()
    {
        var ds = new DisjointSet<string>(["A", "B", "C"]);

        Assert.Equal(3, ds.ElementCount);
        Assert.Equal(3, ds.SetCount);
    }

    [Fact(DisplayName = "Constructor with non-collection IEnumerable creates correct DisjointSet")]
    public void Constructor_WhenGivenNonCollectionEnumerable_ShouldCreateCorrectDisjointSet()
    {
        IEnumerable<int> source = Enumerable.Range(1, 5);

        var ds = new DisjointSet<int>(source);

        Assert.Equal(5, ds.ElementCount);
        Assert.Equal(5, ds.SetCount);
    }

    [Fact(DisplayName = "Constructor with duplicate elements throws ArgumentException")]
    public void Constructor_WhenDuplicateElements_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new DisjointSet<string>(["A", "B", "A"]));
    }

    [Fact(DisplayName = "Constructor with null comparer throws ArgumentNullException")]
    public void Constructor_WhenNullComparer_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DisjointSet<string>(["A"], null!));
    }

    [Fact(DisplayName = "Constructor with custom comparer uses it to determine element equality")]
    public void Constructor_WhenCustomComparer_ShouldUseComparerForElementEquality()
    {
        var ds = new DisjointSet<string>(
            ["paris", "LYON"],
            StringComparer.OrdinalIgnoreCase);

        Assert.Throws<ArgumentException>(() => ds.MakeSet("PARIS"));
    }

    // -------------------------------------------------------------------------
    // MakeSet
    // -------------------------------------------------------------------------

    [Fact(DisplayName = "MakeSet with new element increases ElementCount and SetCount by one")]
    public void MakeSet_WhenNewElement_ShouldIncreaseElementCountAndSetCountByOne()
    {
        var ds = new DisjointSet<string>(["A", "B"]);

        ds.MakeSet("C");

        Assert.Equal(3, ds.ElementCount);
        Assert.Equal(3, ds.SetCount);
    }

    [Fact(DisplayName = "MakeSet creates a singleton set containing only the new element")]
    public void MakeSet_WhenNewElement_ShouldCreateSingletonSet()
    {
        var ds = new DisjointSet<string>(["A"]);

        ds.MakeSet("B");
        var set = ds.GetSet("B");

        Assert.Single(set);
        Assert.Contains("B", set);
    }

    [Fact(DisplayName = "MakeSet new element is not in the same set as existing elements")]
    public void MakeSet_WhenNewElement_ShouldNotBeInSameSetAsExistingElements()
    {
        var ds = new DisjointSet<string>(["A"]);

        ds.MakeSet("B");

        Assert.False(ds.InSameSet("A", "B"));
    }

    [Fact(DisplayName = "MakeSet with already existing element throws ArgumentException")]
    public void MakeSet_WhenExistingElement_ShouldThrowArgumentException()
    {
        var ds = new DisjointSet<string>(["A", "B"]);

        Assert.Throws<ArgumentException>(() => ds.MakeSet("A"));
    }

    // -------------------------------------------------------------------------
    // FindSet
    // -------------------------------------------------------------------------

    [Fact(DisplayName = "FindSet on a singleton element returns the element itself")]
    public void FindSet_WhenSingleton_ShouldReturnElementItself()
    {
        var ds = new DisjointSet<string>(["A"]);

        var representative = ds.FindSet("A");

        Assert.Equal("A", representative);
    }

    [Fact(DisplayName = "FindSet returns the same representative for all members of a merged set")]
    public void FindSet_WhenSetMerged_ShouldReturnSameRepresentativeForAllMembers()
    {
        var ds = new DisjointSet<string>(["A", "B", "C"]);
        ds.Union("A", "B");
        ds.Union("B", "C");

        var repA = ds.FindSet("A");
        var repB = ds.FindSet("B");
        var repC = ds.FindSet("C");

        Assert.Equal(repA, repB);
        Assert.Equal(repB, repC);
    }

    [Fact(DisplayName = "FindSet always returns a member of the set")]
    public void FindSet_WhenCalled_ShouldReturnAMemberOfTheSet()
    {
        var ds = new DisjointSet<string>(["A", "B", "C"]);
        ds.Union("A", "B");

        var representative = ds.FindSet("A");
        var members = ds.GetSet("A");

        Assert.Contains(representative, members);
    }

    [Fact(DisplayName = "FindSet after Union is former representative of one of the two merged sets")]
    public void FindSet_AfterUnion_ShouldReturnFormerRepresentativeOfOneOfTheMergedSets()
    {
        var ds = new DisjointSet<string>(["A", "B", "C", "D"]);
        var repX = ds.FindSet("A");
        var repY = ds.FindSet("C");

        ds.Union("A", "C");
        var repAfter = ds.FindSet("A");

        Assert.True(repAfter == repX || repAfter == repY);
    }

    [Fact(DisplayName = "FindSet is stable for elements whose set is not involved in a Union")]
    public void FindSet_WhenUnrelatedUnionOccurs_ShouldRemainStable()
    {
        var ds = new DisjointSet<string>(["A", "B", "C"]);
        var repBefore = ds.FindSet("A");

        ds.Union("B", "C");

        Assert.Equal(repBefore, ds.FindSet("A"));
    }

    [Fact(DisplayName = "FindSet with unknown element throws ArgumentException")]
    public void FindSet_WhenUnknownElement_ShouldThrowArgumentException()
    {
        var ds = new DisjointSet<string>(["A"]);

        Assert.Throws<ArgumentException>(() => ds.FindSet("Z"));
    }

    // -------------------------------------------------------------------------
    // Union
    // -------------------------------------------------------------------------

    [Fact(DisplayName = "Union of two elements from different sets returns true")]
    public void Union_WhenElementsInDifferentSets_ShouldReturnTrue()
    {
        var ds = new DisjointSet<string>(["A", "B"]);

        var result = ds.Union("A", "B");

        Assert.True(result);
    }

    [Fact(DisplayName = "Union of two elements already in the same set returns false")]
    public void Union_WhenElementsAlreadyInSameSet_ShouldReturnFalse()
    {
        var ds = new DisjointSet<string>(["A", "B"]);
        ds.Union("A", "B");

        var result = ds.Union("A", "B");

        Assert.False(result);
    }

    [Fact(DisplayName = "Union of an element with itself returns false")]
    public void Union_WhenSameElementBothSides_ShouldReturnFalse()
    {
        var ds = new DisjointSet<string>(["A"]);

        var result = ds.Union("A", "A");

        Assert.False(result);
    }

    [Fact(DisplayName = "Union of different sets decrements SetCount by one")]
    public void Union_WhenDifferentSets_ShouldDecrementSetCountByOne()
    {
        var ds = new DisjointSet<string>(["A", "B", "C"]);

        ds.Union("A", "B");

        Assert.Equal(2, ds.SetCount);
    }

    [Fact(DisplayName = "Union of same set does not change SetCount")]
    public void Union_WhenSameSet_ShouldNotChangeSetCount()
    {
        var ds = new DisjointSet<string>(["A", "B"]);
        ds.Union("A", "B");
        var countBefore = ds.SetCount;

        ds.Union("A", "B");

        Assert.Equal(countBefore, ds.SetCount);
    }

    [Fact(DisplayName = "Union does not change ElementCount")]
    public void Union_WhenCalled_ShouldNotChangeElementCount()
    {
        var ds = new DisjointSet<string>(["A", "B", "C"]);

        ds.Union("A", "B");
        ds.Union("B", "C");

        Assert.Equal(3, ds.ElementCount);
    }

    [Fact(DisplayName = "Union is transitive: A-B then B-C puts all three elements in the same set")]
    public void Union_WhenCalledTransitively_ShouldPutAllElementsInSameSet()
    {
        var ds = new DisjointSet<string>(["A", "B", "C"]);

        ds.Union("A", "B");
        ds.Union("B", "C");

        Assert.True(ds.InSameSet("A", "C"));
    }

    [Fact(DisplayName = "Union merges set members correctly — all are accessible via GetSet")]
    public void Union_WhenCalled_ShouldMergeSetMembersCorrectly()
    {
        var ds = new DisjointSet<string>(["A", "B", "C", "D"]);
        ds.Union("A", "B");
        ds.Union("C", "D");

        ds.Union("A", "C");
        var merged = ds.GetSet("A");

        Assert.Equal(4, merged.Count);
        Assert.Contains("A", merged);
        Assert.Contains("B", merged);
        Assert.Contains("C", merged);
        Assert.Contains("D", merged);
    }

    [Fact(DisplayName = "Union with unknown first element throws ArgumentException")]
    public void Union_WhenFirstElementUnknown_ShouldThrowArgumentException()
    {
        var ds = new DisjointSet<string>(["A"]);

        Assert.Throws<ArgumentException>(() => ds.Union("Z", "A"));
    }

    [Fact(DisplayName = "Union with unknown second element throws ArgumentException")]
    public void Union_WhenSecondElementUnknown_ShouldThrowArgumentException()
    {
        var ds = new DisjointSet<string>(["A"]);

        Assert.Throws<ArgumentException>(() => ds.Union("A", "Z"));
    }

    [Fact(DisplayName = "Union reaches SetCount of one after all elements are unioned")]
    public void Union_WhenAllElementsUnioned_ShouldReduceSetCountToOne()
    {
        var ds = new DisjointSet<string>(["A", "B", "C", "D"]);

        ds.Union("A", "B");
        ds.Union("C", "D");
        ds.Union("A", "C");

        Assert.Equal(1, ds.SetCount);
    }

    [Fact(DisplayName = "Union uses representative from the larger set")]
    public void Union_WhenSetsHaveDifferentSizes_ShouldUseRepresentativeFromLargerSet()
    {
        // Arrange — build a set of 3 {A,B,C} and a singleton {D}
        var ds = new DisjointSet<string>(["A", "B", "C", "D"]);
        ds.Union("A", "B");
        ds.Union("A", "C");
        // {A,B,C} representative = A (A was always the larger side)
        var largerSetRep = ds.FindSet("A");

        // Act — merge the singleton D into the large set
        ds.Union("A", "D");

        // Assert — representative must come from the larger set
        Assert.Equal(largerSetRep, ds.FindSet("D"));
    }

    // -------------------------------------------------------------------------
    // InSameSet
    // -------------------------------------------------------------------------

    [Fact(DisplayName = "InSameSet returns false for two elements in distinct initial sets")]
    public void InSameSet_WhenDistinctInitialSets_ShouldReturnFalse()
    {
        var ds = new DisjointSet<string>(["A", "B"]);

        Assert.False(ds.InSameSet("A", "B"));
    }

    [Fact(DisplayName = "InSameSet returns true for an element checked against itself")]
    public void InSameSet_WhenSameElement_ShouldReturnTrue()
    {
        var ds = new DisjointSet<string>(["A"]);

        Assert.True(ds.InSameSet("A", "A"));
    }

    [Fact(DisplayName = "InSameSet returns true after the two elements are unioned")]
    public void InSameSet_WhenElementsAreUnioned_ShouldReturnTrue()
    {
        var ds = new DisjointSet<string>(["A", "B"]);

        ds.Union("A", "B");

        Assert.True(ds.InSameSet("A", "B"));
    }

    [Fact(DisplayName = "InSameSet is symmetric — order of arguments does not matter")]
    public void InSameSet_WhenCheckedBothWays_ShouldBeSymmetric()
    {
        var ds = new DisjointSet<string>(["A", "B"]);
        ds.Union("A", "B");

        Assert.Equal(ds.InSameSet("A", "B"), ds.InSameSet("B", "A"));
    }

    [Fact(DisplayName = "InSameSet is transitive — A-B and B-C implies A-C")]
    public void InSameSet_WhenTransitiveUnions_ShouldReturnTrueForIndirectMembers()
    {
        var ds = new DisjointSet<string>(["A", "B", "C"]);
        ds.Union("A", "B");
        ds.Union("B", "C");

        Assert.True(ds.InSameSet("A", "C"));
    }

    [Fact(DisplayName = "InSameSet returns false for elements in distinct sets after partial unions")]
    public void InSameSet_WhenPartialUnions_ShouldReturnFalseForElementsInDifferentSets()
    {
        var ds = new DisjointSet<string>(["A", "B", "C", "D"]);
        ds.Union("A", "B");
        ds.Union("C", "D");

        Assert.False(ds.InSameSet("A", "C"));
    }

    [Fact(DisplayName = "InSameSet with unknown element throws ArgumentException")]
    public void InSameSet_WhenUnknownElement_ShouldThrowArgumentException()
    {
        var ds = new DisjointSet<string>(["A"]);

        Assert.Throws<ArgumentException>(() => ds.InSameSet("A", "Z"));
    }

    // -------------------------------------------------------------------------
    // GetSet
    // -------------------------------------------------------------------------

    [Fact(DisplayName = "GetSet on a singleton returns a collection containing only that element")]
    public void GetSet_WhenSingleton_ShouldReturnCollectionWithOnlyThatElement()
    {
        var ds = new DisjointSet<string>(["A", "B"]);

        var set = ds.GetSet("A");

        Assert.Single(set);
        Assert.Contains("A", set);
    }

    [Fact(DisplayName = "GetSet after union returns all members of the merged set")]
    public void GetSet_WhenSetMerged_ShouldReturnAllSetMembers()
    {
        var ds = new DisjointSet<string>(["A", "B", "C"]);
        ds.Union("A", "B");
        ds.Union("A", "C");

        var set = ds.GetSet("B");

        Assert.Equal(3, set.Count);
        Assert.Contains("A", set);
        Assert.Contains("B", set);
        Assert.Contains("C", set);
    }

    [Fact(DisplayName = "GetSet called on different members of the same set returns equivalent contents")]
    public void GetSet_WhenCalledOnDifferentMembersOfSameSet_ShouldReturnEquivalentContents()
    {
        var ds = new DisjointSet<string>(["A", "B", "C"]);
        ds.Union("A", "B");
        ds.Union("B", "C");

        var setFromA = ds.GetSet("A");
        var setFromC = ds.GetSet("C");

        Assert.Equal(setFromA.OrderBy(x => x), setFromC.OrderBy(x => x));
    }

    [Fact(DisplayName = "GetSet with unknown element throws ArgumentException")]
    public void GetSet_WhenUnknownElement_ShouldThrowArgumentException()
    {
        var ds = new DisjointSet<string>(["A"]);

        Assert.Throws<ArgumentException>(() => ds.GetSet("Z"));
    }

    // -------------------------------------------------------------------------
    // GetAllSets
    // -------------------------------------------------------------------------

    [Fact(DisplayName = "GetAllSets on empty DisjointSet returns empty enumerable")]
    public void GetAllSets_WhenEmpty_ShouldReturnEmptyEnumerable()
    {
        var ds = new DisjointSet<string>([]);

        Assert.Empty(ds.GetAllSets());
    }

    [Fact(DisplayName = "GetAllSets on initial state returns one singleton set per element")]
    public void GetAllSets_WhenNoUnionPerformed_ShouldReturnOneSingletonSetPerElement()
    {
        var ds = new DisjointSet<string>(["A", "B", "C"]);

        var allSets = ds.GetAllSets().ToList();

        Assert.Equal(3, allSets.Count);
        Assert.All(allSets, set => Assert.Single(set));
    }

    [Fact(DisplayName = "GetAllSets after all unions returns exactly one set containing all elements")]
    public void GetAllSets_WhenAllElementsUnioned_ShouldReturnSingleSetWithAllElements()
    {
        var ds = new DisjointSet<string>(["A", "B", "C", "D"]);
        ds.Union("A", "B");
        ds.Union("C", "D");
        ds.Union("A", "C");

        var allSets = ds.GetAllSets().ToList();

        Assert.Single(allSets);
        Assert.Equal(4, allSets[0].Count);
    }

    [Fact(DisplayName = "GetAllSets after partial unions returns correct number of sets with correct members")]
    public void GetAllSets_WhenPartialUnions_ShouldReturnCorrectSets()
    {
        // Arrange
        var ds = new DisjointSet<string>(["A", "B", "C", "D", "E"]);

        // Act
        ds.Union("A", "B");  // {A,B}, {C}, {D}, {E}
        ds.Union("C", "D");  // {A,B}, {C,D}, {E}

        // Assert
        var allSets = ds.GetAllSets()
            .Select(s => s.OrderBy(x => x).ToList())
            .OrderBy(s => s[0])
            .ToList();

        Assert.Equal(3, allSets.Count);
        Assert.Equal(["A", "B"], allSets[0]);
        Assert.Equal(["C", "D"], allSets[1]);
        Assert.Equal(["E"], allSets[2]);
    }

    // -------------------------------------------------------------------------
    // SetCount / ElementCount invariants
    // -------------------------------------------------------------------------

    [Fact(DisplayName = "SetCount equals ElementCount when no Union has been performed")]
    public void SetCount_WhenNoUnionPerformed_ShouldEqualElementCount()
    {
        var ds = new DisjointSet<string>(["A", "B", "C", "D"]);

        Assert.Equal(ds.ElementCount, ds.SetCount);
    }

    [Fact(DisplayName = "SetCount decrements by exactly one for each effective Union")]
    public void SetCount_WhenMultipleEffectiveUnions_ShouldDecrementByOneEachTime()
    {
        var ds = new DisjointSet<int>([1, 2, 3, 4, 5]);

        ds.Union(1, 2);
        Assert.Equal(4, ds.SetCount);

        ds.Union(3, 4);
        Assert.Equal(3, ds.SetCount);

        ds.Union(1, 3);
        Assert.Equal(2, ds.SetCount);
    }

    [Fact(DisplayName = "ElementCount does not change after any combination of Union and MakeSet")]
    public void ElementCount_WhenUnionAndMakeSetCalled_ShouldOnlyChangeOnMakeSet()
    {
        var ds = new DisjointSet<string>(["A", "B", "C"]);

        ds.Union("A", "B");
        Assert.Equal(3, ds.ElementCount);

        ds.MakeSet("D");
        Assert.Equal(4, ds.ElementCount);

        ds.Union("A", "C");
        Assert.Equal(4, ds.ElementCount);
    }

    // -------------------------------------------------------------------------
    // Real-world scenario — cycle detection
    // -------------------------------------------------------------------------

    [Fact(DisplayName = "Cycle detection: adding edge between already-connected vertices identifies a cycle")]
    public void Union_WhenEdgeConnectsAlreadyConnectedVertices_ShouldDetectCycle()
    {
        // Arrange — graph vertices 0..4, edges forming a cycle on 1-2-3
        var ds = new DisjointSet<int>([0, 1, 2, 3, 4]);
        (int From, int To)[] edges = [(0, 1), (1, 2), (2, 3), (3, 1)];
        int? cycleEdge = null;

        // Act
        foreach (var (from, to) in edges)
        {
            if (!ds.Union(from, to))
            {
                cycleEdge = from * 10 + to;
                break;
            }
        }

        // Assert
        Assert.Equal(31, cycleEdge); // edge 3→1 closes the cycle
    }

    // -------------------------------------------------------------------------
    // Real-world scenario — Kruskal-like MST grouping
    // -------------------------------------------------------------------------

    [Fact(DisplayName = "Kruskal scenario: processing sorted edges builds correct connected components")]
    public void Union_WhenProcessingEdgesInWeightOrder_ShouldBuildCorrectConnectedComponents()
    {
        // Arrange — 5 vertices, edges by ascending weight
        var ds = new DisjointSet<int>([0, 1, 2, 3, 4]);
        (int From, int To, int Weight)[] edges =
        [
            (0, 1, 1),
            (1, 2, 2),
            (3, 4, 3),
            (0, 2, 4), // would create cycle if added
            (2, 3, 5),
        ];

        var mstEdges = new List<(int, int)>();

        // Act
        foreach (var (from, to, _) in edges)
        {
            if (ds.Union(from, to))
            {
                mstEdges.Add((from, to));
            }
        }

        // Assert — 4 edges for 5 vertices, all in one component
        Assert.Equal(4, mstEdges.Count);
        Assert.Equal(1, ds.SetCount);
        Assert.True(ds.InSameSet(0, 4));
        Assert.DoesNotContain((0, 2), mstEdges); // cycle edge was skipped
    }
}