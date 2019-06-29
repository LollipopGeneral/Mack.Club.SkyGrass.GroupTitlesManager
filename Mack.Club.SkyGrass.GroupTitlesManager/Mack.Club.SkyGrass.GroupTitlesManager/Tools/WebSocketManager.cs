using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;

namespace Mack.Club.SkyGrass.GroupTitlesManager.Tools
{
    public class WebSocketManager
    {
        private WebSocket _ws = null;
        private List<long> _qqGroupIds = null;
        private List<long> _qqManagerIdArr = null;
        private bool _isConnecting = false;
        private string _webSocketUrl = "";


        private Dictionary<string, List<int>> clientMsgIdDic = new Dictionary<string, List<int>>();

        public void Init(string webSocketUrl, List<long> qqGroupIdArr, List<long> qqManagerIdArr)
        {
            _webSocketUrl = webSocketUrl;
            _qqGroupIds = qqGroupIdArr;
            _qqManagerIdArr = qqManagerIdArr;
            initWs();
        }

        private void initWs()
        {
            _ws = new WebSocket(_webSocketUrl);

            _ws.OnMessage += (sender, e) =>
            {
                OnMessage(e);
            };

            _ws.OnOpen += (sender, e) =>
            {
                Console.WriteLine("Ws.open");
                _isConnecting = true;
            };

            _ws.OnError += (sender, e) =>
            {
                Console.WriteLine("Ws.error");
                _isConnecting = false;
                while (true)
                {
                    if (_isConnecting)
                    {
                        break;
                    }

                    initWs();

                    Thread.Sleep(10 * 1000);
                }
            };

            _ws.OnClose += (sender, e) =>
            {
                Console.WriteLine("Ws.close");
                _isConnecting = false;
                while (true)
                {
                    if (_isConnecting)
                    {
                        break;
                    }

                    initWs();

                    Thread.Sleep(10 * 1000);
                }
            };

            _ws.Connect();
        }

        private void OnMessage(MessageEventArgs e)
        {
            string jsonStr = e.Data;

            try
            {
                GroupMessage wsGroupMsg = JsonConvert.DeserializeObject<GroupMessage>(jsonStr);
                //Console.WriteLine($"{JsonConvert.SerializeObject(_qqGroupIds)}_{JsonConvert.SerializeObject(_qqManagerIdArr)}");
                //Console.WriteLine($"{wsGroupMsg.sub_type}_{wsGroupMsg.group_id}_{wsGroupMsg.user_id}");
                if (wsGroupMsg.sub_type == "normal")
                {
                    if (_qqGroupIds.Contains(wsGroupMsg.group_id))
                    {
                        if (_qqManagerIdArr.Contains(wsGroupMsg.user_id))
                        {
                            // 是否为命令
                            if (wsGroupMsg.raw_message.Contains("设置专属头衔"))
                            {
                                Console.WriteLine(jsonStr);
                                // 切割命令参数 
                                // 设置专属头衔 QQ号 头衔
                                var msg = wsGroupMsg.raw_message.Trim();
                                var arr = msg.Split(' ');
                                if(arr.Length == 3)
                                {
                                    string cmd = arr[0];
                                    if (cmd.Trim().Equals("设置专属头衔"))
                                    {
                                        long qq = Convert.ToInt64(arr[1]);
                                        string title = arr[2];
                                        SetTitles(wsGroupMsg.group_id, qq, title);
                                        File.AppendAllLines("SetTitlesInfo.log", new List<string>() { $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}\t设置专属头衔\t{wsGroupMsg.group_id}_{wsGroupMsg.user_id} SET {qq}: {title}" }, Encoding.UTF8);
                                    }
                                }
                            }
                            else if(wsGroupMsg.raw_message.Contains("取消专属头衔"))
                            {
                                Console.WriteLine(jsonStr);
                                // 切割命令参数 
                                // 取消专属头衔 QQ号
                                var msg = wsGroupMsg.raw_message.Trim();
                                var arr = msg.Split(' ');
                                if (arr.Length == 2)
                                {
                                    string cmd = arr[0];
                                    if (cmd.Trim().Equals("取消专属头衔"))
                                    {
                                        long qq = Convert.ToInt64(arr[1]);
                                        string title = "";
                                        SetTitles(wsGroupMsg.group_id, qq, title);
                                        File.AppendAllLines("SetTitlesInfo.log", new List<string>() { $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}\t取消专属头衔\t{wsGroupMsg.group_id}_{wsGroupMsg.user_id} SET {qq}: {title}" }, Encoding.UTF8);
                                    }                                    
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
            }

        }

        public void SetTitles(long group_id, long user_id, string special_title)
        {
            if (_isConnecting == false)
            {
                return;
            }
            try
            {

                #region 设置头衔
                WebSocketMsg wsMsg = new WebSocketMsg();
                wsMsg.action = WebSocketMsg.set_group_special_title;

                SetGroupSpecialTitleParams msg = new SetGroupSpecialTitleParams();
                msg.group_id = group_id;
                msg.user_id = user_id;
                msg.special_title = special_title.Length > 6 ? special_title.Substring(0, 6) : special_title;

                wsMsg.@params = msg;

                _ws.Send(JsonConvert.SerializeObject(wsMsg));

                #endregion
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                File.AppendAllText("Error.log", ex.StackTrace, Encoding.UTF8);
            }

        }

    }

    public class WsData
    {
        public Data data { get; set; }
        public string echo { get; set; }
        public int retcode { get; set; }
        public string status { get; set; }
    }

    public class Data
    {
        public int message_id { get; set; }
    }

    class WebSocketMsg
    {
        public const string send_private_msg = "send_private_msg";
        public const string send_group_msg = "send_group_msg";
        public const string delete_msg = "delete_msg";
        public const string set_group_special_title = "set_group_special_title";

        public string action { get; set; }
        public IParamsMsg @params { get; set; }
        public string echo { get; set; }
    }

    class DeleteParams : IParamsMsg
    {
        public int message_id { get; set; }
    }

    interface IParamsMsg
    {

    }

    class GroupParams : IParamsMsg
    {
        public long group_id { get; set; }
        public string message { get; set; }
    }

    class SetGroupSpecialTitleParams : IParamsMsg
    {
        public long group_id { get; set; }
        public long user_id { get; set; }
        public string special_title { get; set; }
        public int duration = -1;
    }

    #region HTTP_API_GroupMessage

    public class GroupMessage
    {
        public object anonymous { get; set; }
        public int font { get; set; }
        public int group_id { get; set; }
        public string message { get; set; }
        public int message_id { get; set; }
        public string message_type { get; set; }
        public string post_type { get; set; }
        public string raw_message { get; set; }
        public int self_id { get; set; }
        public Sender sender { get; set; }
        public string sub_type { get; set; }
        public int time { get; set; }
        public int user_id { get; set; }
    }

    public class Sender
    {
        public int age { get; set; }
        public string area { get; set; }
        public string card { get; set; }
        public string level { get; set; }
        public string nickname { get; set; }
        public string role { get; set; }
        public string sex { get; set; }
        public string title { get; set; }
        public int user_id { get; set; }
    }

    #endregion
}
