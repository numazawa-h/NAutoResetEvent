using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NCommonUtility
{
    /// <summary>
    /// AutoResetEventのラッパークラス
    /// 【機能】
    /// 1. Cancel
    ///   自動でCancellationTokenを生成し、Cancel() で待機中のタスクを全てキャンセル
    /// 2. 待機順にタスクを実行
    ///   コンストラクタやReset()の引数指定により、Wait() した順にタスクが実行されるようになる
    /// </summary>
    public class NAutoResetEvent
    {
        private bool _init_sw;
        private bool _keep_order;
        private AutoResetEvent _event;
        private CancellationTokenSource _token_source;
        private CancellationToken _token;
        private int _seq_no = 0;
        private Queue<int> _seq = new Queue<int>();
        private bool _canceled = false;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="init">初期状態指定(true:シグナル状態, false:非シグナル状態)</param>
        /// <param name="keep_order">Wait順保持</param>
        public NAutoResetEvent(bool init=false, bool keep_order=false)
        {
            _init_sw = init;
            _keep_order = keep_order;
            _event = new AutoResetEvent(init);
            _token_source = new CancellationTokenSource();
            _token = _token_source.Token;
        }

        /// <summary>
        /// eventがsetされるのを待つ
        /// </summary>
        /// <returns>途中でキャンセルされたらfalse</returns>
        public async Task<bool> Wait()
        {
            Task<bool> task;
            var tkn = _token;
            task = Task<bool>.Run(() =>
            {
                int seq;
                lock (this)
                {
                    seq = ++_seq_no;
                    if (_keep_order)
                    {
                        _seq.Enqueue(seq);
                    }
                }
                bool exit = false;
                while (exit == false)
                {
                    if (tkn.IsCancellationRequested)
                    {
                        break;
                    }
                    exit = _event.WaitOne(50);
                    if (exit && _keep_order)
                    {
                        lock (this)
                        {
                            // 順番に実行する（先頭でなければskip）
                            if (_seq.Peek() < seq)
                            {
                                exit = false;
                                _event.Set();
                            }
                        }
                    }
                }
                lock (this)
                {
                    if (_keep_order)
                    {
                        _seq.Dequeue();
                    }
                    else
                    {
                        --_seq_no;
                    }
                }
                return exit;
            });
            return await task;
        }

        /// <summary>
        /// eventをset
        /// </summary>
        public void Set()
        {
            _event.Set();
        }

        /// <summary>
        /// キャンセル
        /// </summary>
        public void Cancel()
        {
            lock (this)
            {
                _canceled = true;
                _token_source.Cancel();
            }
            // 全てのTaskがキャンセルされるのを待つ
            while (true)
            {
                if (_keep_order && _seq.Count == 0)
                {
                    break;
                }
                if (_keep_order==false && _seq_no == 0)
                {
                    break;
                }
                System.Threading.Thread.Sleep(10);
            }
        }
        public void Reset()
        {
            Reset(_init_sw, _keep_order);
        }

        /// <summary>
        /// リセット
        /// </summary>
        /// <param name="init">初期状態指定(true:シグナル状態, false:非シグナル状態)</param>
        /// <param name="keep_order">Wait順保持</param>
        public void Reset(bool init, bool keep_order)
        {
            if (_canceled == false)
            {
                Cancel();
            }
            lock (this)
            {
                _token_source.Dispose();
                _canceled = false;
                _init_sw = init;
                _keep_order = keep_order;
                _seq_no = 0;
                _token_source = new CancellationTokenSource();
                _token = _token_source.Token;
                if (_init_sw)
                {
                    _event.Set();
                }
                else
                {
                    _event.Reset();
                }
            }
        }
        public bool isCanceled()
        {
            return _canceled;
        }
    }
}
