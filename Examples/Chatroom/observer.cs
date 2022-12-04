using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using MergeSharp;
using MergeSharp.TCPConnectionManager;
using Microsoft.Extensions.Logging;

public class UpdateReporter : IObserver<ReceivedSyncUpdateInfo>
{
    private Guid chatroom_id;
    private ManualResetEvent waitHandle;

    public UpdateReporter(Guid chatroom_id, ManualResetEvent waitHandle)
    {
        this.chatroom_id = chatroom_id;
        this.waitHandle = waitHandle;
    }

    public virtual void OnCompleted()
    {
    }

    public virtual void OnError(Exception error)
    {
    }

    public virtual void OnNext(ReceivedSyncUpdateInfo value)
    {
        this.waitHandle.Set();
    }
}