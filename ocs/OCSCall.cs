using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Rtc.Collaboration;
using Microsoft.Rtc.Signaling;
using System.Threading;
using OCSCallerCSharp.help;
using System.Web;
using System.Net.Mime;

namespace OCSCallerCSharp
{
    public class OCSCall
    {
        
        private OCSHelper _helper;
        private UserEndpoint _userEndpoint;
        private AutoResetEvent _OCSCompletedEvent = new AutoResetEvent(false);
        private String _conversationSubject = "Test Notice!";
        private String _conversationPriority = ConversationPriority.Urgent;
        private InstantMessagingCall[] _instantMessagingCall = null;
        //private InstantMessagingFlow _instantMessagingFlow;
        private String _messageToSend = "Hello Test";

        private int _sendCount = 0;
        private int _sentTotal = 0;

        // Event to notify application main thread on completion of the sample.
 

        public OCSCall(){
        }

        public OCSCall(OCSCall source)
        {
        }

        static void Main(string[] args)
        {
            //String.OCSCall ocsCall = new OCSCall();
            //ocsCall.sendMessage("xxxxx");
        }

        /**
         *
         */
        public void sendMessage(String subject,String content,String sendtos)
        {
            _sendCount = 0;
            
            if (subject!=null)
            {
                _conversationSubject = subject;
            }
            if (content != null)
            {
                String htmlMessage = "<html><body ><div>" + content + "</div></body></html>";
                _messageToSend = htmlMessage;
            }
            Exception ex = null;
            try
            {
                //Create the UserEndpoint 
                _helper = new OCSHelper();
                _userEndpoint = _helper.CreateEstablishedUserEndpoint();

                //Setup the conversation and place the call
                ConversationSettings convSettings = new ConversationSettings();
                convSettings.Priority = _conversationPriority;
                convSettings.Subject = _conversationSubject;


                if (sendtos != null)
                {
                    String[] sendtoArray = sendtos.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    if (sendtoArray != null && sendtoArray.Length > 0)
                    {
                        _sentTotal = sendtoArray.Length;
                        _instantMessagingCall = new InstantMessagingCall[_sentTotal];
                        for (int i = 0; i < sendtoArray.Length; i++)
                        {
                            Conversation conversation = new Conversation(_userEndpoint, convSettings);
                            _instantMessagingCall[i] = new InstantMessagingCall(conversation);
                            _instantMessagingCall[i].StateChanged += this.InstantMessagingCall_StateChanged;
                            _instantMessagingCall[i].InstantMessagingFlowConfigurationRequested +=
                             this.InstantMessagingCall_FlowConfigurationRequested;
                            String _sendToAccount = "sip:" + sendtoArray[i] + "@mediatek.com";
                            _instantMessagingCall[i].BeginEstablish(_sendToAccount, new ToastMessage("Hello Toast"), null,
                             CallEstablishCompleted, _instantMessagingCall[i]);
                        }                      
                    }
                }
               
            }
            catch (InvalidOperationException iOpEx)
            {
                ex = iOpEx;
            }finally{
                if (ex != null)
                {
                    _helper.log(ex.ToString());
                    _helper.log("Shutting down platform due to error");
                    _helper.ShutdownPlatform();
                }
            }
            _OCSCompletedEvent.WaitOne();
        }

        void InstantMessagingCall_StateChanged(object sender, CallStateChangedEventArgs e)
        {
            _helper.log("Call has changed state. The previous call state was: " + e.PreviousState +
                "and the current state is: " + e.State);
        }

        public void InstantMessagingCall_FlowConfigurationRequested(object sender,
           InstantMessagingFlowConfigurationRequestedEventArgs e)
        {
            Console.WriteLine("Flow Created.");
            //xueming 
            InstantMessagingFlow  _instantMessagingFlow = e.Flow;

            // Now that the flow is non-null, bind the event handlers for State 
            // Changed and Message Received. When the flow goes active, 
            // (as indicated by the state changed event) the program will send 
            // the IM in the event handler.
            _instantMessagingFlow.StateChanged += this.InstantMessagingFlow_StateChanged;

            // Message Received is the event used to indicate that a message has
            // been received from the far end.
            _instantMessagingFlow.MessageReceived += this.InstantMessagingFlow_MessageReceived;

            // Also, here is a good place to bind to the 
            // InstantMessagingFlow.RemoteComposingStateChanged event to receive
            // typing notifications of the far end user.
            _instantMessagingFlow.RemoteComposingStateChanged +=
                                                    this.InstantMessagingFlow_RemoteComposingStateChanged;
        }

        private void InstantMessagingFlow_StateChanged(object sender, MediaFlowStateChangedEventArgs e)
        {
            Console.WriteLine("Flow state changed from " + e.PreviousState + " to " + e.State);
            // When flow is active, media operations (here, sending an IM) 
            // may begin.
            //xueming
            InstantMessagingFlow instantMessagingFlow = sender as InstantMessagingFlow;

            if (e.State == MediaFlowState.Active)
            {
                // Send the message on the InstantMessagingFlow.
               // _instantMessagingFlow.BeginSendInstantMessage(_messageToSend, SendMessageCompleted,
                //    _instantMessagingFlow);
                string str = _messageToSend.ToString();
                byte[] bytes = Encoding.UTF8.GetBytes(str);
                instantMessagingFlow.BeginSendInstantMessage(new ContentType("text/html"), bytes, SendMessageCompleted,
                  instantMessagingFlow);
                
            }
        }

        private void InstantMessagingFlow_RemoteComposingStateChanged(object sender,
                                                                        ComposingStateChangedEventArgs e)
        {
            // Prints the typing notifications of the far end user.
            Console.WriteLine("Participant "
                                + e.Participant.Uri.ToString()
                                + " is "
                                + e.ComposingState.ToString()
                                );
        }

        private void InstantMessagingFlow_MessageReceived(object sender, InstantMessageReceivedEventArgs e)
        {
            //xueming 
            InstantMessagingFlow _instantMessagingFlow = sender as InstantMessagingFlow;
            
            // On an incoming Instant Message, print the contents to the console.
            Console.WriteLine(e.Sender.Uri + " said: " + e.TextBody);

            // Shutdown if the far end tells us to.
            if (e.TextBody.Equals("bye", StringComparison.OrdinalIgnoreCase))
            {
                // Shutting down the platform will terminate all attached objects.
                // If this was a production application, it would tear down the 
                // Call/Conversation, rather than terminating the entire platform.
                _instantMessagingFlow.BeginSendInstantMessage("Shutting Down...", SendMessageCompleted,
                    _instantMessagingFlow);
                _helper.ShutdownPlatform();
                _OCSCompletedEvent.Set();
            }
            else
            {
                // Echo the instant message back to the far end (the sender of 
                // the instant message).
                // Change the composing state of the local end user while sending messages to the far end.
                // A delay is introduced purposely to demonstrate the typing notification displayed by the 
                // far end client; otherwise the notification will not last long enough to notice.
                _instantMessagingFlow.LocalComposingState = ComposingState.Composing;
                Thread.Sleep(2000);

                //Echo the message with an "Echo" prefix.
                _instantMessagingFlow.BeginSendInstantMessage("Echo: " + e.TextBody, SendMessageCompleted,
                    _instantMessagingFlow);
            }

        }

        private void SendMessageCompleted(IAsyncResult result)
        {
            InstantMessagingFlow instantMessagingFlow = result.AsyncState as InstantMessagingFlow;
            Exception ex = null;
            try
            {
                instantMessagingFlow.EndSendInstantMessage(result);
                _sendCount += 1;
                _helper.log("The message has been sent.");
            }
            catch (OperationTimeoutException opTimeEx)
            {
                // OperationFailureException: Indicates failure to connect the 
                // IM to the remote party due to timeout (called party failed to
                // respond within the expected time).
                // TODO (Left to the reader): Write real error handling code.
                ex = opTimeEx;
            }
            catch (RealTimeException rte)
            {
                // Other errors may cause other RealTimeExceptions to be thrown.
                // TODO (Left to the reader): Write real error handling code.
                ex = rte;
            }
            finally
            {
                // Reset the composing state of the local end user so that the typing notifcation as seen 
                // by the far end client disappears.
                //_instantMessagingFlow.LocalComposingState = ComposingState.Idle;
                instantMessagingFlow.LocalComposingState = ComposingState.Idle;
                if (ex != null)
                {
                    // If the action threw an exception, terminate the sample, 
                    // and print the exception to the console.
                    // TODO (Left to the reader): Write real error handling code.
                    Console.WriteLine(ex.ToString());
                    Console.WriteLine("Shutting down platform due to error");
                    _helper.ShutdownPlatform();
                }

                //xueming
                if (_sendCount >= _sentTotal)
                {
                     _helper.log("Shutting down platform after send all Message!");
                    _OCSCompletedEvent.Set();
                    _helper.ShutdownPlatform();  
                }
           
            }
        }

        private void CallEstablishCompleted(IAsyncResult result)
        {
            InstantMessagingCall instantMessagingCall = result.AsyncState as InstantMessagingCall;
            Exception ex = null;
            try
            {
                instantMessagingCall.EndEstablish(result);
                Console.WriteLine("The call is now in the established state.");
            }
            catch (OperationFailureException opFailEx)
            {
                // OperationFailureException: Indicates failure to connect the 
                // call to the remote party.
                // TODO (Left to the reader): Write real error handling code.
                ex = opFailEx;
            }
            catch (RealTimeException rte)
            {
                // Other errors may cause other RealTimeExceptions to be thrown.
                // TODO (Left to the reader): Write real error handling code.
                ex = rte;
            }
            finally
            {
                if (ex != null)
                {
                    // If the action threw an exception, terminate the sample, 
                    // and print the exception to the console.
                    // TODO (Left to the reader): Write real error handling code.
                    Console.WriteLine(ex.ToString());
                    Console.WriteLine("Shutting down platform due to error");
                    _helper.ShutdownPlatform();
                }
            }
        }

    }
}