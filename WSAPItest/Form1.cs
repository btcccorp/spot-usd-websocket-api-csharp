using System;
using System.Drawing;
using System.Windows.Forms;
using ProExchange.Common;
using ProExchange.Common.Security;
using ProExchange.JSON.API;
using ProExchange.JSON.API.Notifications;
using ProExchange.JSON.API.Requests;
using ProExchange.JSON.API.Responses;
using WebSocketSharp;
using WSAPItest;

namespace WSAPITest
{

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            this.MinimumSize = new Size(600, 400);
        }

        // Append text in sent box
        public void AppendSenderBox(string value)
        {
            if (!value.Contains(Environment.NewLine))
            {
                value = value + " " + DateTime.Now + "\r\n";
            }
            if (InvokeRequired)
            {
                try
                {
                    this.Invoke(new Action<string>(AppendSenderBox), new object[] { value });
                }
                catch (ObjectDisposedException ex)
                {

                }
                return;
            }
            textBox1.Text += value;

            textBox1.SelectionStart = textBox1.Text.Length;
            textBox1.ScrollToCaret();
        }

        // Append text in receive box
        public void AppendReceiverBox(string value)
        {
            if (!value.Contains(Environment.NewLine))
            {
                value = value + " " + DateTime.Now + "\r\n";
            }
            if (InvokeRequired)
            {
                this.Invoke(new Action<string>(AppendReceiverBox), new object[] { value });
                return;
            }
            textBox2.Text += value;

            textBox2.SelectionStart = textBox2.Text.Length;
            textBox2.ScrollToCaret();
        }

        // Instantiate global objects
        private static readonly Credentials.ICredentials credentials = new Credentials.AccessKey(WS.ACCESS_KEY, WS.SECRET_KEY); // calculate credentials using API keys
        public WebSocket ws;  // declare websocket connection object
        private readonly JsonSerializer<Request> serializer = new JsonSerializer<Request>(); // instantiate websocket message serializer
        private readonly JsonDeserializer<JsonMessageOut> deserializer = new JsonDeserializer<JsonMessageOut>(); // instantiate websocket message deserializer
        
        // Generate timestamp
        private long UnixTimeNow()
        {
            var timeSpan = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
            return (long)timeSpan.TotalSeconds;
        }

        // only selected elements from response objects are printed, you should customize and explore them.
        #region Response handlers
        private void DisplayCancelReplaceOrderResponse(CancelReplaceOrderResponse cancelReplaceOrderResponse)
        {
            AppendReceiverBox($"CancelReplaceOrderResponse - ResultCode: {cancelReplaceOrderResponse.ResultCode} , OrderId: {cancelReplaceOrderResponse.OrderID}");
        }

        private void DisplayGetOrdersResponse(GetOrdersResponse getOrdersResponse)
        {
            AppendReceiverBox($"GetOrderResponse[0] - price: {getOrdersResponse.Reports[0].Price}, vol: {getOrdersResponse.Reports[1].LeaveQty}");
        }

        private void DisplayPlaceOrderResponse(PlaceOrderResponse placeOrderResponse)
        {
            AppendReceiverBox($"PlaceOrderResponse - Text: {placeOrderResponse.Text}, OrdID: {placeOrderResponse.OrderId}, OrdRejReason: {placeOrderResponse.OrdRejReason}, ResultCode: {placeOrderResponse.ResultCode}");
        }

        private void DisplayCancelOrderResponse(CancelOrderResponse cancelOrderResponse)
        {
            AppendReceiverBox($"CancelOrderResponse - OrdID: {cancelOrderResponse.OrderID}, ResultCode: {cancelOrderResponse.ResultCode}");
        }

        private void DisplayExecReport(ExecReport execReport)
        {
            AppendReceiverBox($"ExecutionReport - Price: {execReport.Price}, Qty: {execReport.LeaveQty}, Text: {execReport.Text}");
        }

        private void DisplayGetActiveContractsResponse(GetActiveContractsResponse getActiveContractsResponse)
        {
            AppendReceiverBox($"GetActiveContractsResponse[0] - Price: {getActiveContractsResponse.Contracts[0].Price}, Qty: {getActiveContractsResponse.Contracts[0].Quantity}, Count: {getActiveContractsResponse.Contracts.Count}");
        }

        private void DisplayOrderBook(OrderBook obj)
        {
            AppendReceiverBox($"OrderBook[0] - Price: {obj.List[0].Price}, Side: {obj.List[0].Side}, Size: {obj.List[0].Size}");
        }

        private void DisplayAccount(AccountInfo info)
        {
            AppendReceiverBox($"AccountInfoResponse - Cash: {info.BalanceList[0].Cash}, Currency: {info.BalanceList[0].Currency}");
        }

        private void DisplayTicker(Ticker ticker)
        {
            AppendReceiverBox($"TickerResponse - Open: {ticker.Open}, Last: {ticker.Last}, High: {ticker.High}, Low: {ticker.Low}");
        } 
        #endregion 


        // Sign() will calculate relevant account signiture information and insert into your message request body
        private Request Sign(SignedRequest request)
        {
            request.ClientRequestId = Guid.NewGuid().ToString("N");
            request.Account = credentials.Account;
            request.Date = DateTime.UtcNow.ToString("yyyyMMdd");
            request.Signature = SignatureEngine.ComputeSignature(
                SignatureEngine.ComputeHash(credentials.Key),
                SignatureEngine.PrepareMessage(request));
            return request;
        }


        #region Button Actions

        // Login Button
        private void button1_Click(object sender, EventArgs e)
        {
            ws = new WebSocket(WS.APIURL); // initialize websocket connection object

            // add specified response type listeners to deserializer
            deserializer.On<Ticker>(DisplayTicker);
            deserializer.On<AccountInfo>(DisplayAccount);
            deserializer.On<GetAccountInfoResponse>(response => DisplayAccount(response.AccountInfo));
            deserializer.On<ExecReport>(DisplayExecReport);
            deserializer.On<OrderBook>(DisplayOrderBook);
            deserializer.On<QuoteResponse>(response => DisplayOrderBook(response.OrderBook));
            deserializer.On<PlaceOrderResponse>(DisplayPlaceOrderResponse);
            deserializer.On<CancelOrderResponse>(DisplayCancelOrderResponse);
            deserializer.On<GetOrdersResponse>(DisplayGetOrdersResponse);
            deserializer.On<CancelReplaceOrderResponse>(DisplayCancelReplaceOrderResponse);
            deserializer.On<GetActiveContractsResponse>(DisplayGetActiveContractsResponse);

            // Login when openning websocket connection
            ws.OnOpen += (a, b) =>
            {
                // Send login request
                ws.Send(serializer.Serialize(Sign(new LoginRequest())));
                AppendSenderBox($"LoginRequest() to {WS.APIURL}");
            };

            // Let deserializer process response messages upon receive
            ws.OnMessage += (a, b) => deserializer.Deserialize(b.Data);

            // Error processing when something wrong with websocket connection
            ws.OnError += (a, b) =>
            {
                AppendReceiverBox($"An error occured - Message: {b.Message} , Exception: {b.Exception}");
            };

            // Do something when websocket connection close
            ws.OnClose += (a, b) =>
            {

                AppendReceiverBox($"Connection closed: {b.Reason}");
            };

            ws.Connect(); // start connecting to remote websocket API server

            AppendReceiverBox("Connected");
        }

        // QuoteReq Button
        private void button3_Click(object sender, EventArgs e)
        {
            ws.Send(serializer.Serialize(new QuoteRequest() { Symbol = WS.SYMBOL, QuoteType = 2 }));
            AppendSenderBox($"QuoteRequest()");
        }

        // AccInfoReq Button
        private void button4_Click(object sender, EventArgs e)
        {
            ws.Send(serializer.Serialize(Sign(new GetAccountInfoRequest())));
            AppendSenderBox($"GetAccountInfoRequest()");
        }

        // PlaceOrderReq Button
        private void button5_Click(object sender, EventArgs e)
        {
            ws.Send(serializer.Serialize(Sign(new PlaceOrderRequest()
            {
                ClientRequestId = Guid.NewGuid().ToString("N"),
                Symbol = "BTCUSD",
                Side = OrderSide.Sell,
                OrderType = OrderType.LIMIT,
                Quantity = 0.01M,
                Price = 723,
                StopPrice = 0,
                TIF = TimeInForce.DAY,
                ExprDate = 0,
                ExprTime = new TimeSpan()
            })));
            AppendSenderBox($"PlaceOrderRequest()");
        }

        // CancelOrderReq Button
        private void button6_Click(object sender, EventArgs e)
        {
            ws.Send(serializer.Serialize(Sign(new CancelOrderRequest()
            {
                ClientRequestId = Guid.NewGuid().ToString("N"),
                OrdID = "cdc3883ceb2a43b89e98923fbe10f0d6",
                Symbol = "BTCUSD"
            })));
            AppendSenderBox($"CancelOrderRequest()");
        }

        // OpenOrder Button
        private void button7_Click(object sender, EventArgs e)
        {
            ws.Send((serializer.Serialize(Sign(new GetOrdersRequest()
            {
                ClientRequestId = Guid.NewGuid().ToString("N"),
                Begin = 0,
                Date = "20161104",
                End = UnixTimeNow() * 1000,
                Symbol = "BTCUSD",
                Status = "A,0,1,2",
                Type = "C"
            }))));
            AppendSenderBox($"GetOrdersRequest() - OpenOrders");
        }

        // ClosedOrder Button
        private void button8_Click(object sender, EventArgs e)
        {
            ws.Send((serializer.Serialize(Sign(new GetOrdersRequest()
            {
                ClientRequestId = Guid.NewGuid().ToString("N"),
                Begin = 0,
                Date = "20161104",
                End = UnixTimeNow() * 1000,
                Symbol = "BTCUSD",
                Status = "3,S",
                Type = "L"
            }))));
            AppendSenderBox($"GetOrdersRequest() - ClosedOrders");
        }

        private void button9_Click(object sender, EventArgs e)
        {

        }

        // Logout Button
        private void button2_Click(object sender, EventArgs e)
        {
            ws.Close();
            AppendReceiverBox("websocket connection closed");
        }

        // CancelAll Button
        private void button10_Click(object sender, EventArgs e)
        {
            ws.Send(serializer.Serialize(Sign(new CancelAllOrdersRequest()
            {
                ClientRequestId = Guid.NewGuid().ToString("N"),
                Date = "20161107",
                HighPrice = 750,
                LowPrice = 600,
                Side = OrderSide.Buy,
                Symbol = "BTCUSD"
            })));
        }
    } 
    #endregion


    // settings
    public static class WS
    {
        public const string APIURL = "wss://spotusd-wsp.btcc.com";
        public const string SYMBOL = "BTCUSD";
        public const string BPI = "BPIUSD";
        public const string ACCESS_KEY = ""; // Your Access_key
        public const string SECRET_KEY = ""; // Your Secret_key
    }
}
