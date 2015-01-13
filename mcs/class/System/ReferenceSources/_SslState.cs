//
// Mono-specific additions to Microsoft's _SslState.cs
//
namespace System.Net.Security {
    using System.IO;
    using System.Net.Sockets;

    partial class SslState
    {
        internal IAsyncResult BeginShutdown (bool waitForReply, AsyncCallback asyncCallback, object asyncState)
        {
            LazyAsyncResult lazyResult = new LazyAsyncResult(this, asyncState, asyncCallback);
            ShutdownAsyncProtocolRequest asyncRequest = new ShutdownAsyncProtocolRequest(waitForReply, lazyResult);
            ProtocolToken message = Context.CreateShutdownMessage();
            StartWriteShutdown(message.Payload, asyncRequest);
            return lazyResult;
        }

        internal void EndShutdown(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
            {
                throw new ArgumentNullException("asyncResult");
            }

            LazyAsyncResult lazyResult = asyncResult as LazyAsyncResult;
            if (lazyResult == null)
            {
                throw new ArgumentException(SR.GetString(SR.net_io_async_result, asyncResult.GetType().FullName), "asyncResult");
            }

            // No "artificial" timeouts implemented so far, InnerStream controls timeout.
            lazyResult.InternalWaitForCompletion();

            if (lazyResult.Result is Exception)
            {
                if (lazyResult.Result is IOException)
                {
                    throw (Exception)lazyResult.Result;
                }
                throw new IOException(SR.GetString(SR.mono_net_io_shutdown), (Exception)lazyResult.Result);
            }
        }

        void StartWriteShutdown(byte[] buffer, AsyncProtocolRequest asyncRequest)
        {
            // request a write IO slot
            if (CheckEnqueueWrite(asyncRequest))
            {
                // operation is async and has been queued, return.
                return;
            }

            byte[] lastHandshakePayload = null;
            if (LastPayload != null)
                throw new NotImplementedException();

            if (asyncRequest != null)
            {
                // prepare for the next request
                IAsyncResult ar = ((NetworkStream)InnerStream).BeginWrite(buffer, 0, buffer.Length, ShutdownCallback, asyncRequest);
                if (!ar.CompletedSynchronously)
                    return;

                ((NetworkStream)InnerStream).EndWrite(ar);
            }
            else
            {
                ((NetworkStream)InnerStream).Write(buffer, 0, buffer.Length);
            }

            // release write IO slot
            FinishWrite();

            if (asyncRequest != null)
                asyncRequest.CompleteUser();
        }

        static void ShutdownCallback(IAsyncResult transportResult)
        {
            if (transportResult.CompletedSynchronously)
                return;

            ShutdownAsyncProtocolRequest asyncRequest = (ShutdownAsyncProtocolRequest)transportResult.AsyncState;

            SslState sslState = (SslState)asyncRequest.AsyncObject;
            try
            {
                ((NetworkStream)(sslState.InnerStream)).EndWrite(transportResult);
                sslState.FinishWrite();
                if (asyncRequest.WaitForReply)
                    sslState.WaitForShutdown(asyncRequest);
                else
                    asyncRequest.CompleteUser();
            }
            catch (Exception e)
            {
                if (asyncRequest.IsUserCompleted)
                {
                    // This will throw on a worker thread.
                    throw;
                }
                sslState.FinishWrite();
                asyncRequest.CompleteWithError(e);
            }
        }

        static void ShutdownReadCallback(IAsyncResult transportResult)
        {
            ShutdownAsyncProtocolRequest asyncRequest = (ShutdownAsyncProtocolRequest)transportResult.AsyncState;

            var buffer = new byte [8];
            SslState sslState = (SslState)asyncRequest.AsyncObject;
            try {
                var ret = sslState.SecureStream.EndRead(transportResult);
                if (ret != 0)
                    throw new IOException (SR.GetString(SR.mono_net_io_shutdown));
                sslState.FinishRead(null);
                asyncRequest.CompleteUser();
            } catch (Exception e) {
                if (asyncRequest.IsUserCompleted) {
                    // This will throw on a worker thread.
                    throw;
                }
                sslState.FinishRead(null);
                asyncRequest.CompleteWithError(e);
            }
        }

        void WaitForShutdown(AsyncProtocolRequest asyncRequest)
        {
            var buffer = new byte[16];
            IAsyncResult ar = SecureStream.BeginRead(buffer, 0, buffer.Length, ShutdownReadCallback, asyncRequest);
            if (ar.CompletedSynchronously)
                ShutdownReadCallback(ar);
        }

        class ShutdownAsyncProtocolRequest: AsyncProtocolRequest
        {
            public readonly bool WaitForReply;

            internal ShutdownAsyncProtocolRequest(bool wairForReply, LazyAsyncResult userAsyncResult): base (userAsyncResult)
            {
                WaitForReply = wairForReply;
            }
        }
    }
}
