using Xunit;
using MergeSharp;
using System.Collections.Generic;
using System.Threading;

namespace MergeSharp.Tests;

public class GVecTests
{
    [Fact]
    public void TestGVecSingle()
    {
        GVector<string> vec = new();

        vec.Add("a");
        vec.Add("b");
        vec.Add("c");

        Assert.Equal(3, vec.Count);

        Assert.Equal(vec.LookupAll(), new List<string> { "a","b", "c" });


    }

    [Fact]
    public void TestGVecMerge()
    {
        GVector<string> vec = new();
        Thread.Sleep(2);
        GVector<string> vec2 = new();

        vec.Add("a");
        vec.Add("b");

        vec2.Add("c");
        vec2.Add("d");

        vec.Merge((GVectorMsg<string>)vec2.GetLastSynchronizedUpdate());
        vec2.Merge((GVectorMsg<string>)vec.GetLastSynchronizedUpdate());
        Assert.Equal(vec.LookupAll(), vec2.LookupAll());
        Assert.Equal(vec.LookupAll(), new List<string>{ "a", "b", "c", "d" });


    }





}