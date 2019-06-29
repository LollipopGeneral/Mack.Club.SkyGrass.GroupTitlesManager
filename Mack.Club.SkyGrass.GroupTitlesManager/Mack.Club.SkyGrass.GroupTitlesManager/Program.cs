using Mack.Club.SkyGrass.GroupTitlesManager.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mack.Club.SkyGrass.GroupTitlesManager
{
    class Program
    {
        static WebSocketManager wsManager = new WebSocketManager();
        static void Main(string[] args)
        {
            #region WS URL
            string wsUrl = Convert.ToString(args[0]);
            #endregion

            #region 群参数
            string qqGroups = Convert.ToString(args[1]);
            string[] groups = qqGroups.Split(',');
            List<long> qqGroupIdArr = new List<long>();
            foreach (var item in groups)
            {
                qqGroupIdArr.Add(Convert.ToInt64(item));
            }
            #endregion

            #region 群管理员参数
            string qqManagers = Convert.ToString(args[2]);
            string[] managers = qqManagers.Split(',');
            List<long> qqManagerIdArr = new List<long>();
            foreach (var item in managers)
            {
                qqManagerIdArr.Add(Convert.ToInt64(item));
            }
            #endregion

            wsManager.Init(wsUrl, qqGroupIdArr, qqManagerIdArr);

            Console.ReadLine();
        }
    }
}
