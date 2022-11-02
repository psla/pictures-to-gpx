using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace PicturesToGpx.Test
{
    [TestClass]
    public class EnumerableUtilsTest
    {
        [TestMethod]
        public void TestOneArray()
        {
            CollectionAssert.AreEqual(new[] { 1 }, EnumerableUtils.Merge(new[] { 1 }, new int[0], (i, j) => i < j).ToList());
            CollectionAssert.AreEqual(new[] { 1 }, EnumerableUtils.Merge(new int[0], new[] { 1 }, (i, j) => i < j).ToList());
        }

        [TestMethod]
        public void TestBothArrays()
        {
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6, 7 }, EnumerableUtils.Merge(new[] { 1, 3, 5 }, new[] { 2, 4, 6, 7 }, (i, j) => i < j).ToList());
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6, 7 }, EnumerableUtils.Merge(new[] { 2, 4, 6, 7 }, new[] { 1, 3, 5 }, (i, j) => i < j).ToList());
        }

        [TestMethod]
        public void TestFirstElementIndexOf()
        {
            Assert.AreEqual(0, new int[] { 7 }.IndexOf(el => el == 7));
        }

        [TestMethod]
        public void TestMissingElement()
        {
            Assert.AreEqual(null, new int[] { 7 }.IndexOf(el => el == 0));
        }

        [TestMethod]
        public void TestMultipleElementsFirstIsReturned()
        {
            Assert.AreEqual(1, new int[] { 7, 6, 6 }.IndexOf(el => el == 6));
        }
    }
}
