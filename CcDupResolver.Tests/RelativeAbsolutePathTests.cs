using System;
using System.IO;
using Xunit;

namespace CcDupResolver.Tests
{
    public class RelativeAbsolutePathTests
    {
        private void RoundTrip(FileInfo fileAbs, DirectoryInfo baseDir, string expectedRel)
        {
            var rel = FileHelper.ToRelative(fileAbs, baseDir);
            Assert.Equal(rel, expectedRel);
            var convAbs = FileHelper.ToAbsolute(rel, baseDir);
            Assert.Equal(convAbs.FullName, fileAbs.FullName);
        }
        [Fact]
        public void Test1()
        {
            RoundTrip(new FileInfo(@"C:\Users\quanb\source\repos\gozeleme\CcDupResolver\CcDupResolver.csproj"),
                new DirectoryInfo(@"C:\Users\quanb\source\repos\CamPreview\CamPreview"),
                @"..\..\gozeleme\CcDupResolver\CcDupResolver.csproj");
        }
        [Fact]
        public void Test2()
        {
            RoundTrip(new FileInfo(@"C:\Users\quanb\source\repos\gozeleme\CcDupResolver\CcDupResolver.csproj"),
                new DirectoryInfo(@"C:\Users\quanb\source\repos\gozeleme"),
                @"CcDupResolver\CcDupResolver.csproj");
        }
        [Fact]
        public void Test3()
        {
            RoundTrip(new FileInfo(@"C:\Users\quanb\abc.txt"),
                new DirectoryInfo(@"C:\Users\quanb\source\repos\gozeleme\CcDupResolver\"),
                @"..\..\..\..\abc.txt");
        }
        [Fact]
        public void Test4()
        {
            RoundTrip(new FileInfo(@"C:\Users\quanb\source\repos\gozeleme\CcDupResolver\CcDupResolver.csproj"),
                new DirectoryInfo(@"D:\Users\linco\source\repos\gozeleme"),
                @"C:\Users\quanb\source\repos\gozeleme\CcDupResolver\CcDupResolver.csproj");
        }
        [Fact]
        public void Test5()
        {
            RoundTrip(new FileInfo(@"C:\abc.txt"), new DirectoryInfo(@"C:\"), "abc.txt");
        }
    }
}
