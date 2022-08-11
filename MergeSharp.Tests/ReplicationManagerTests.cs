using Xunit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MergeSharp.Tests;


public class ReplicationManagersPNCTests : IDisposable
{

    ReplicationManager rm0, rm1;

    public ReplicationManagersPNCTests()
    {
            DummyNode n0 = new DummyNode(0);
            DummyNode n1 = new DummyNode(1);

            DummyNode[] nodes = {n0, n1};

            DummyConnectionManager cm0 = new DummyConnectionManager(nodes, 0);
            DummyConnectionManager cm1 = new DummyConnectionManager(nodes, 1);

            n0.recivingConnectionManager = cm0;
            n1.recivingConnectionManager = cm1;

            this.rm0 = new ReplicationManager(cm0);
            this.rm1 = new ReplicationManager(cm1);

            this.rm0.RegisterType<PNCounter>();
            this.rm1.RegisterType<PNCounter>();
    }


    public void Dispose()
    {
    }





    [Fact]
    public void RepManagerPNCTest()
    {
        Guid uid = this.rm0.CreateCRDTInstance<PNCounter>(out PNCounter pnc1);

        pnc1.Increment(5);
        pnc1.Decrement(8);
        pnc1.Increment(10);
        pnc1.Decrement(3);

        var pnc1r = rm1.GetCRDT<PNCounter>(uid);   

        Assert.Equal(pnc1.Get(), pnc1r.Get());

    }

    
    [Fact]
    public void RepManagerPNCTest1()
    {
        // Create 4 PNCounters on each replication manager
        Guid uid0 = this.rm0.CreateCRDTInstance<PNCounter>(out PNCounter pnc0);
        Guid uid1 = this.rm1.CreateCRDTInstance<PNCounter>(out PNCounter pnc1);
        Guid uid2 = this.rm0.CreateCRDTInstance<PNCounter>(out PNCounter pnc2);
        Guid uid3 = this.rm1.CreateCRDTInstance<PNCounter>(out PNCounter pnc3);

        // Increment each PNCounter by a random amount, repeat 5 times
        Random rnd = new Random();
        for (int i = 0; i < 5; i++)
        {
            pnc0.Increment(rnd.Next(0, 100));
            pnc1.Increment(rnd.Next(0, 100));
            pnc2.Increment(rnd.Next(0, 100));
            pnc3.Increment(rnd.Next(0, 100));
        }

        // check if the same pncounters are replicated on both nodes
        var pnc0r = rm1.GetCRDT<PNCounter>(uid0);
        var pnc1r = rm1.GetCRDT<PNCounter>(uid1);
        var pnc2r = rm0.GetCRDT<PNCounter>(uid2);
        var pnc3r = rm0.GetCRDT<PNCounter>(uid3);

        Assert.Equal(pnc0.Get(), pnc0r.Get());
        Assert.Equal(pnc1.Get(), pnc1r.Get());
        Assert.Equal(pnc2.Get(), pnc2r.Get());
        Assert.Equal(pnc3.Get(), pnc3r.Get());
    }

    [Fact]
    public void RepManagerPNCTestLocalConcurrentWrites()
    {

        Guid uid = this.rm0.CreateCRDTInstance<PNCounter>(out PNCounter pnc1);

        var pnc1r = rm1.GetCRDT<PNCounter>(uid);


        // start a task that will increment the counter
        Task t1 = new Task(() =>
        {
            for (int i = 0; i < 10; i++)
            {
                pnc1.Increment(1);
            }
        });

        // increment on the replica
        for (int i = 0; i < 10; i++)
        {
            pnc1r.Increment(1);
        }

        // wait for the task to finish
        t1.Wait(5000);

        Assert.Equal(pnc1.Get(), pnc1r.Get());
    }
}

public class ReplicationManagers2PSetTests : IDisposable
{

    ReplicationManager rm0, rm1;

    public ReplicationManagers2PSetTests()
    {
            DummyNode n0 = new DummyNode(0);
            DummyNode n1 = new DummyNode(1);

            DummyNode[] nodes = {n0, n1};

            DummyConnectionManager cm0 = new DummyConnectionManager(nodes, 0);
            DummyConnectionManager cm1 = new DummyConnectionManager(nodes, 1);

            n0.recivingConnectionManager = cm0;
            n1.recivingConnectionManager = cm1;

            this.rm0 = new ReplicationManager(cm0);
            this.rm1 = new ReplicationManager(cm1);

            this.rm0.RegisterType<TPSet<string>>();
            this.rm1.RegisterType<TPSet<string>>();
    }

    [Fact]
    public void RegisterMultipleTypesTest()
    {
        rm0.RegisterType<PNCounter>();
        rm1.RegisterType<PNCounter>();
    }

    public void Dispose()
    {
    }
}