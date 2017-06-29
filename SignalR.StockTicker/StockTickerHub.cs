using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace SignalR.StockTicker
{
    [HubName("stockTickerMini")]
    public class StockTickerHub : Hub
    {
        private readonly StockTicker _stockTicker;

        /// <summary>
        /// 用户的connectionID与用户名对照表
        /// </summary>
        private IList<KeyValuePair<string, IDToken>> _connections =
            new List<KeyValuePair<string, IDToken>>();

        public StockTickerHub() : this(StockTicker.Instance) { }

        public StockTickerHub(StockTicker stockTicker)
        {
            _stockTicker = stockTicker;
        }

        public IEnumerable<Stock> GetAllStocks()
        {
            return _stockTicker.GetAllStocks();
        }

        /// <summary>
        /// 用户上线函数
        /// </summary>
        /// <param name="openID"></param>
        /// <param name="wid"></param>
        public void SendLogin(string wid, string openid)
        {
            KeyValuePair<string, IDToken> temp = 
                new KeyValuePair<string, IDToken>(Context.ConnectionId, new IDToken() { Wid = wid, openid = openid });
            if (!_connections.Contains(temp))
            {
                _connections.Add(temp);
            }
            else
            {
                //_connections[name] = Context.ConnectionId;
            }
        }
    }
}