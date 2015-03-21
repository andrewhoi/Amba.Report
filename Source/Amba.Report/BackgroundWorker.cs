// Copyright 2015, Vladimir Kuznetsov. All rights reserved.
//
// This file is part of "Amba.Report" library.
// 
// "Amba.Report" library is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// "Amba.Report" library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License
// along with "Amba.Report" library. If not, see <http://www.gnu.org/licenses/>

namespace Amba.Report
{
    using Microsoft.Owin;
    using Owin;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    /// <summary>
    /// Background worker class
    /// </summary>
    public class BackgroundWorker : IDisposable
    {
        private Action action;
        private readonly TimeSpan interval;
        private CancellationTokenSource tokenSource;
        private readonly string name;
        private Task task;

        /// <summary>
        /// Initialize instance
        /// </summary>
        /// <param name="action"></param>
        /// <param name="interval"></param>
        /// <param name="name"></param>
        public BackgroundWorker(Action action, TimeSpan interval, string name)
        {
            this.action = action;
            this.interval = interval;
            this.name = name;
        }
        /// <summary>
        /// Start background worker
        /// </summary>
        public void Start()
        {

            tokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = tokenSource.Token;
            task = Task.Run(async () =>
            {
                while (true)
                {
                    action();
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Delay(interval, cancellationToken);
                }
            }, cancellationToken);

            //logger.Info("Background worker with name '{0}' started.", name);
        }
        /// <summary>
        /// Stop background worker
        /// </summary>
        public void Stop()
        {
            tokenSource.Cancel();
            try
            {
                task.Wait();
            }
            catch (AggregateException e)
            {
                foreach (var v in e.InnerExceptions)
                {
                    //logger.Trace(e.Message + " " + v.Message);
                }
            }
            //logger.Info("Background worker with name '{0}' stopped.", name);
        }
        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            Stop();
            action = null;
        }

    }



}
