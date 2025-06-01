using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using TidyTrader.ApiIntegration.Models;
using System.Net.WebSockets;

namespace TidyTrader.Tests
{
    public class WebSocketTests
    {
        private readonly string _apiKey;
        private readonly string _apiSecret;
        private readonly string _privateUrl = "wss://fapi.bitunix.com/private/";
        private readonly string _publicUrl = "wss://fapi.bitunix.com/public/";

        public WebSocketTests()
        {
            // Obtain your real API credentials from environment variables (recommended) 
            // or replace with literal strings for testing.
            _apiKey = "dd9ac82aaedec750922f3e6fc5438816";
            _apiSecret = "4ac673c254b5affa65549a2ed5f25c76";
        }

        [Fact]
        public async Task Connect_SubscribeToBalance_ReceivesBalanceUpdate()
        {
            var client = new BitunixWebSocketClient(_apiKey, _apiSecret, _privateUrl);

            List<BitunixBalanceData> receivedBalances = null;
            var updateReceived = new TaskCompletionSource<bool>();

            // 1) Capture the OnBalanceUpdate event
            client.OnBalanceUpdate += balances =>
            {
                receivedBalances = balances;
                Console.WriteLine("=== Balance Update Received ===");
                foreach (var b in balances)
                {
                    Console.WriteLine($"Coin: {b.Coin}, Available: {b.Available}, Frozen: {b.Frozen}");
                }
                updateReceived.TrySetResult(true);  
            };

            // 2) Connect (will open and authenticate under the hood)
            await client.ConnectAsync();
            Assert.Equal(WebSocketState.Open, client.WebSocketState);

            // 3) Subscribe to the balance channel
            await client.SubscribeToBalanceAsync();

            // 4) Wait up to 10s for an update
            //var timeout = Task.Delay(TimeSpan.FromSeconds(10));
            //var winner = await Task.WhenAny(updateReceived.Task, timeout);
            //Assert.True(winner == updateReceived.Task, "Did not receive a balance update within 10 seconds.");

            await Task.Delay(10000);
            // 5) Verify we got at least one balance entry
            //Assert.NotNull(receivedBalances);
            //Assert.NotEmpty(receivedBalances);

            // 6) Cleanly close
            await client.CloseAsync();
            Assert.Equal(WebSocketState.Closed, client.WebSocketState);
        }

        [Fact]
        public async Task Connect_SubscribeToOrderChannel_ReceivesOrderUpdate()
        {
            var client = new BitunixWebSocketClient(_apiKey, _apiSecret, _privateUrl);

            List<BitunixOrderData> received = null;
            var tcs = new TaskCompletionSource<bool>();

            client.OnOrderUpdate += orders =>
            {
                received = orders;
                tcs.TrySetResult(true);
            };

            // 1) connect & authenticate
            await client.ConnectAsync();
            Assert.Equal(WebSocketState.Open, client.WebSocketState);

            // 2) subscribe to order events (you may need to generate an order on your account to see it)
            await client.SubscribeToOrderAsync("BTCUSDT");

            // 3) wait up to 10s
            //var winner = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(10)));
            //ve Assert.True(winner == tcs.Task, "Did not receive an order update within 10s.");

            // 4) sanity check
            // Assert.NotNull(received);
            //Assert.NotEmpty(received);
            await Task.Delay(15);
            // 5) tear down
            await client.CloseAsync();
            Assert.Equal(WebSocketState.Closed, client.WebSocketState);
        }

        [Fact]
        public async Task Connect_SendPing_ReceivesPong()
        {
            var client = new BitunixWebSocketClient(_apiKey, _apiSecret, _privateUrl);

            // We'll capture the pong timestamp here
            long? pongTimestamp = null;
            var pongReceived = new TaskCompletionSource<bool>();

            client.OnPong += ts =>
            {
                pongTimestamp = ts;
                Console.WriteLine($"[TEST] Pong received: {ts}");
                pongReceived.TrySetResult(true);
            };

            // 1) Connect + authenticate
            await client.ConnectAsync();
            Assert.Equal(WebSocketState.Open, client.WebSocketState);

            // 2) Send ping
            //await client.SendPingAsync();
            Console.WriteLine("[TEST] Ping sent, waiting for pong...");

            // 3) Wait up to 5s for the pong
            var winner = await Task.WhenAny(pongReceived.Task, Task.Delay(TimeSpan.FromSeconds(5)));
            Assert.True(winner == pongReceived.Task, "Did not receive pong within 5 seconds.");

            // 4) Verify we got a reasonable timestamp
            Assert.NotNull(pongTimestamp);
            Assert.True(pongTimestamp > 0, "Pong timestamp should be a positive Unix time.");

            // 5) Cleanup
            await client.CloseAsync();
            Assert.Equal(WebSocketState.Closed, client.WebSocketState);
        }

        [Fact]
        public async Task DepthBook_Subscription_PrintsUpdates_UntilKeypress()
        {
            var client = new BitunixWebSocketClient(_apiKey, _apiSecret, _publicUrl);

            client.OnDepthUpdate += book =>
            {
                Console.WriteLine($"--- {book.Channel} @ {book.Symbol}  ts={book.Timestamp} ---");
                Console.WriteLine("  BIDS:");
                foreach (var lvl in book.Data.Bids)
                    Console.WriteLine($"    {lvl[0],10} × {lvl[1]}");
                Console.WriteLine("  ASKS:");
                foreach (var lvl in book.Data.Asks)
                    Console.WriteLine($"    {lvl[0],10} × {lvl[1]}");
                Console.WriteLine();
            };

            Console.WriteLine("Connecting & authenticating…");
            await client.ConnectAsync();
            Console.WriteLine("Subscribing to depth_book1 for BTCUSDT…");
            await client.SubscribeToDepthAsync("HBARUSDT", "depth_book15");

            Console.WriteLine("Press any key to stop…");
            Console.ReadKey();

            Console.WriteLine("Closing…");
            await client.CloseAsync();
            client.Dispose();
        }

        [Fact]
        public async Task PriceChannel_Subscription_PrintsUpdates_UntilKeypress()
        {
            var client = new BitunixWebSocketClient(_apiKey, _apiSecret, _publicUrl);

            client.OnPriceUpdate += pu =>
            {
                Console.WriteLine($"--- PRICE @ {pu.Symbol} ({pu.Timestamp}) ---");
                Console.WriteLine($" MarketPrice: {pu.Data.MarketPrice}");
                Console.WriteLine($"  IndexPrice: {pu.Data.IndexPrice}");
                Console.WriteLine($" FundingRate: {pu.Data.FundingRate}");
                Console.WriteLine($"  SettleTime: {pu.Data.FundingRateSettleTime}");
                Console.WriteLine($" NextSettle: {pu.Data.NextFundingSettleTime}");
                Console.WriteLine();
            };

            Console.WriteLine("Connecting & authenticating…");
            await client.ConnectAsync();

            Console.WriteLine("Subscribing to price for BTCUSDT…");
            await client.SubscribeToPriceAsync("BTCUSDT");

            Console.WriteLine("Press any key to stop…");
            Console.ReadKey();

            Console.WriteLine("Closing connection…");
            await client.CloseAsync();
            client.Dispose();
        }

        [Fact]
        public async Task Connect_SubscribeToTicker_ReceivesTickerUpdate()
        {
            var client = new BitunixWebSocketClient(_apiKey, _apiSecret, _publicUrl);

            BitunixTickerData receivedTicker = null;
            var tickerReceived = new TaskCompletionSource<bool>();

            // 1) Hook up the OnTickerUpdate event
            client.OnTickerUpdate += ticker =>
            {
                receivedTicker = ticker;
                Console.WriteLine("=== Ticker Update Received ===");
                Console.WriteLine($"O:{ticker.Open} H:{ticker.High} L:{ticker.Low} Last:{ticker.Last}");
                tickerReceived.TrySetResult(true);
            };

            // 2) Connect
            await client.ConnectAsync();
            Assert.Equal(WebSocketState.Open, client.WebSocketState);

            // 3) Subscribe to rolling‐24h ticker for BTCUSDT
            await client.SubscribeToTickerAsync("BTCUSDT");

            Console.ReadKey();

            Console.WriteLine("Closing…");
            await client.CloseAsync();
            client.Dispose();
        }

        [Fact]
        public async Task Connect_SubscribeToAggregatedTickers_ReceivesUpdate()
        {
            var client = new BitunixWebSocketClient(_apiKey, _apiSecret, _publicUrl);

            List<BitunixTickerItem> received = null;
            var gotUpdate = new TaskCompletionSource<bool>();

            client.OnTickersUpdate += items =>
            {
                received = items;
                gotUpdate.TrySetResult(true);
            };

            // 1) open & authenticate
            await client.ConnectAsync();
            Assert.Equal(WebSocketState.Open, client.WebSocketState);

            // 2) subscribe to BTCUSDT and ETHUSDT aggregate‐tickers
            await client.SubscribeToAggregatedTickersAsync("BTCUSDT", "ETHUSDT");

            Console.ReadKey();

            Console.WriteLine("Closing…");
            await client.CloseAsync();
            client.Dispose();
            Assert.Equal(WebSocketState.Closed, client.WebSocketState);
        }

        [Fact]
        public async Task Connect_SubscribeToTradeChannel_ReceivesTradeUpdate()
        {
            var client = new BitunixWebSocketClient(_apiKey, _apiSecret, _publicUrl);

            List<BitunixTradeData> received = null;
            var updateTcs = new TaskCompletionSource<bool>();

            client.OnTradeUpdate += trades =>
            {
                received = trades;
                Console.WriteLine("=== Trade Update Received ===");
                foreach (var t in trades)
                {
                    Console.WriteLine($"  {t.Timestamp} {t.Side} {t.Volume}@{t.Price}");
                }
                updateTcs.TrySetResult(true);
            };

            // 1) Connect & handshake
            await client.ConnectAsync();
            Assert.Equal(WebSocketState.Open, client.WebSocketState);

            // 2) Subscribe to the public trade feed for BTCUSDT
            await client.SubscribeToTradeAsync("BTCUSDT");

            Console.ReadKey();

            Console.WriteLine("Closing…");
            await client.CloseAsync();
            client.Dispose();
            Assert.Equal(WebSocketState.Closed, client.WebSocketState);
        }
    }
}

