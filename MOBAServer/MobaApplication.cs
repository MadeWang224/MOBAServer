﻿using ExitGames.Logging;
using ExitGames.Logging.Log4Net;
using Photon.SocketServer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MOBAServer
{
    public class MobaApplication : ApplicationBase
    {
        /// <summary>
        /// 客户端连接
        /// </summary>
        /// <param name="initRequest"></param>
        /// <returns></returns>
        protected override PeerBase CreatePeer(InitRequest initRequest)
        {
            return new MobaClient(initRequest);
        }
        /// <summary>
        /// 服务器初始化
        /// </summary>
        protected override void Setup()
        {
            InitLogging();
        }
        /// <summary>
        /// 服务器断开
        /// </summary>
        protected override void TearDown()
        {
            
        }

        #region 日志功能

        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 初始化日志
        /// </summary>
        private void InitLogging()
        {
            LogManager.SetLoggerFactory(Log4NetLoggerFactory.Instance);
            log4net.GlobalContext.Properties["Photon:ApplicationLogPath"] = Path.Combine(this.ApplicationRootPath, "log");
            log4net.GlobalContext.Properties["LogFileName"] =this.ApplicationName;
            log4net.Config.XmlConfigurator.ConfigureAndWatch(new FileInfo(Path.Combine(this.BinaryPath, "log4net.config")));
        }

        /// <summary>
        /// 输出
        /// </summary>
        /// <param name="text"></param>
        public static void LogInfo(string text)
        {
            log.Info(text);
        }
        #endregion
    }
}
