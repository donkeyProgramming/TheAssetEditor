using Editors.CscEditor.Services;

namespace Test.CscEditor
{
    public class CscExternalIdAllocationTests
    {
        [Test]
        public void Allocation_skips_ids_already_used_by_the_host_scene()
        {
            var usedIds = new HashSet<int> { int.MaxValue, int.MaxValue - 1 };
            var nextCandidate = int.MaxValue;

            var id = CscSceneGraphBuilder.AllocateExternalId(usedIds, ref nextCandidate);

            Assert.That(id, Is.EqualTo(int.MaxValue - 2));
            Assert.That(usedIds, Does.Contain(id));
        }

        [Test]
        public void Consecutive_allocations_are_unique()
        {
            var usedIds = new HashSet<int>();
            var nextCandidate = int.MaxValue;

            var first = CscSceneGraphBuilder.AllocateExternalId(usedIds, ref nextCandidate);
            var second = CscSceneGraphBuilder.AllocateExternalId(usedIds, ref nextCandidate);

            Assert.That(second, Is.Not.EqualTo(first));
        }

        [Test]
        public void Allocation_reports_exhaustion_without_wrapping()
        {
            var usedIds = new HashSet<int> { int.MinValue };
            var nextCandidate = int.MinValue;

            Assert.That(
                () => CscSceneGraphBuilder.AllocateExternalId(usedIds, ref nextCandidate),
                Throws.InvalidOperationException);
        }
    }
}
