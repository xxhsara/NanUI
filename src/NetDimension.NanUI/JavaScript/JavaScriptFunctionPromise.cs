using NetDimension.NanUI.Browser.MessagePipe;
using NetDimension.NanUI.JavaScript.Renderer;
using Xilium.CefGlue;

namespace NetDimension.NanUI.JavaScript;




public sealed class JavaScriptFunctionPromise
{
    private bool _isHandled = false;

    private readonly CefFrame frame;
    private readonly Guid uuid;

    internal CefProcessId Side { get; }

    public JavaScriptFunctionPromise(CefFrame frame, Guid uuid, CefProcessId side = CefProcessId.Browser)
    {
        this.frame = frame;
        this.uuid = uuid;

        Side = side;
    }


    public void Resovle(params JavaScriptValue[] retvals)
    {
        if (_isHandled) throw new InvalidOperationException("This method can be only called once.");

        _isHandled = true;

        var arguments = new JavaScriptArray();

        foreach (var retval in retvals)
        {
            arguments.Add(retval);
        }

        if (Side == CefProcessId.Browser)
        {
            var message = new BridgeMessage(JavaScriptExecution.InvokeJavaScriptFunctionHandler.INVOKE_RENDER_SIDE_PROMISE_FUNCTION, JsonSerializer.Serialize(new JavaScriptPromiseFunctionMessageParameter { FuncId = uuid, Success = true, FrameId = frame.Identifier, Data = arguments.ToJson() }));

            FormiumMessageBridge.SendBridgeMessage(CefProcessId.Renderer, frame, message);
        }
        else
        {
            CefRuntime.PostTask(CefThreadId.Renderer, new InvokeJavaScriptPromiseFunctionOnRenderSideTask(frame, uuid)
            {

                Success = true,
                Arguments = arguments,
            });
        }



        CleanPromise();
    }

    public void Reject(string reason = null)
    {
        if (_isHandled) throw new InvalidOperationException("This method can be only called once.");

        _isHandled = true;



        if (Side == CefProcessId.Browser)
        {
            var message = new BridgeMessage(JavaScriptExecution.InvokeJavaScriptFunctionHandler.INVOKE_RENDER_SIDE_PROMISE_FUNCTION, JsonSerializer.Serialize(new JavaScriptPromiseFunctionMessageParameter { FuncId = uuid, Success = false, FrameId = frame.Identifier, Message = reason }));

            FormiumMessageBridge.SendBridgeMessage(CefProcessId.Renderer, frame, message);
        }
        else
        {
            CefRuntime.PostTask(CefThreadId.Renderer, new InvokeJavaScriptPromiseFunctionOnRenderSideTask(frame, uuid)
            {

                Success = false,
                Message = reason
            });
        }



        CleanPromise();

    }

    private void CleanPromise()
    {
        //JavaScriptAsyncFunction.Bag.RemoveWhere(x => x.Uuid == uuid);
    }
}
internal class JavaScriptPromiseFunctionMessageParameter
{
    public Guid FuncId { get; set; }
    public bool Success { get; set; }
    public long FrameId { get; set; }
    public string Data { get; set; }
    public string Message { get; set; }
}
