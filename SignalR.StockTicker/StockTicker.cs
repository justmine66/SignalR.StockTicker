﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace SignalR.StockTicker
{
    public class StockTicker
    {
        // Singleton instance
        private readonly static Lazy<StockTicker> _instance = new Lazy<StockTicker>(() => new StockTicker(GlobalHost.ConnectionManager.GetHubContext<StockTickerHub>().Clients));

        private readonly ConcurrentDictionary<string, Stock> _stocks = new ConcurrentDictionary<string, Stock>();

        private readonly object _updateStockPricesLock = new object();

        //stock can go up or down by a percentage of this factor on each change
        private readonly double _rangePercent = .002;

        private readonly TimeSpan _updateInterval = TimeSpan.FromMilliseconds(250);
        private readonly Random _updateOrNotRandom = new Random();

        private readonly Timer _timer;
        private volatile bool _updatingStockPrices = false;

        private StockTicker(IHubConnectionContext<dynamic> clients)
        {
            Clients = clients;

            _stocks.Clear();
            var stocks = new List<Stock>
            {
                new Stock { Symbol = "阿里巴巴", Price = 30.31m },
                new Stock { Symbol = "百度", Price = 578.18m },
                new Stock { Symbol = "腾讯", Price = 570.30m }
            };
            stocks.ForEach(stock => _stocks.TryAdd(stock.Symbol, stock));

            _timer = new Timer(UpdateStockPrices, null, _updateInterval, _updateInterval);

        }

        public static StockTicker Instance
        {
            get
            {
                return _instance.Value;
            }
        }

        private IHubConnectionContext<dynamic> Clients
        {
            get;
            set;
        }

        public IEnumerable<Stock> GetAllStocks()
        {
            return _stocks.Values;
        }

        private void UpdateStockPrices(object state)
        {
            lock (_updateStockPricesLock)
            {
                if (!_updatingStockPrices)
                {
                    _updatingStockPrices = true;

                    foreach (var stock in _stocks.Values)
                    {
                        if (TryUpdateStockPrice(stock))
                        {
                            BroadcastStockPrice(stock);
                        }
                    }

                    _updatingStockPrices = false;
                }
            }
        }

        private bool TryUpdateStockPrice(Stock stock)
        {
            // Randomly choose whether to update this stock or not
            var r = _updateOrNotRandom.NextDouble();
            if (r > .1)
            {
                return false;
            }

            // Update the stock price by a random factor of the range percent
            var random = new Random((int)Math.Floor(stock.Price));
            var percentChange = random.NextDouble() * _rangePercent;
            var pos = random.NextDouble() > .51;
            var change = Math.Round(stock.Price * (decimal)percentChange, 2);
            change = pos ? change : -change;

            stock.Price += change;
            return true;
        }

        /// <summary>
        /// 更新指定的客户端
        /// </summary>
        /// <param name="connectionId">客户端标识</param>
        /// <param name="stock">股票对象</param>
        private void BroadcastStockPrice(string connectionId, Stock stock)
        {
            Clients.Client(connectionId).updateStockPrice(stock);
        }

        /// <summary>
        /// 广播所有客户端
        /// </summary>
        /// <param name="stock">股票对象</param>
        private void BroadcastStockPrice(Stock stock)
        {
            Clients.All.updateStockPrice(stock);
        }
    }
}