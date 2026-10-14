using DockDAPnext.AppConfiguration;
using DockDAPnext.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace DockDAPnext
{
    public partial class DockDAPwindowControl : UserControl
    {
        public DockDAPwindowControl()
        {
            DiagLog.Log("CTOR: enter, provider=" + (ServiceLocator.Provider != null));

            InitializeComponent();

            if (ServiceLocator.Provider != null)
            {
                var vm = ServiceLocator.Provider.GetService<MainViewModel>();

                this.DataContext = vm;
                DiagLog.Log("CTOR: vm=" + (vm != null));
            }
            else
            {
                DiagLog.Log("CTOR: Provider is NULL - DataContext never set!");
            }

            this.Loaded += (s, e) =>
            {
                DumpButtons("control");
                WireClicks();
                RunRxPipelineTest();
            };
        }

        private readonly System.Collections.Generic.List<System.Windows.Controls.Button> _wired =
            new System.Collections.Generic.List<System.Windows.Controls.Button>();

        // TEMPORARY diagnostics: proves whether WPF raises Click and whether CanExecute is
        // still true at the moment of the click.
        private void WireClicks()
        {
            try
            {
                var queue = new System.Collections.Generic.Queue<DependencyObject>();
                queue.Enqueue(this);
                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(current);
                    for (int i = 0; i < count; i++)
                    {
                        if (System.Windows.Media.VisualTreeHelper.GetChild(current, i) is DependencyObject dobj)
                            queue.Enqueue(dobj);
                    }

                    if (current is System.Windows.Controls.Button btn && !_wired.Contains(btn))
                    {
                        _wired.Add(btn);
                        var b = btn;
                        b.Click += (s, e) =>
                        {
                            string content = (b.Content as TextBlock)?.Text ?? b.Content?.ToString() ?? "?";
                            bool canExec = false;
                            if (b.Command != null)
                            {
                                try { canExec = b.Command.CanExecute(b.CommandParameter); }
                                catch (Exception ex) { DiagLog.Log("CLICK canExec THREW: " + ex.GetType().Name); }
                            }
                            DiagLog.Log("CLICK '" + content + "' cmdNull=" + (b.Command == null)
                                + " canExec=" + canExec + " isEnabled=" + b.IsEnabled);
                        };
                        if (b.Command != null)
                        {
                            var cmd = b.Command;
                            cmd.CanExecuteChanged += (s, e) =>
                            {
                                DiagLog.Log("CANCEXEC-CHANGED cmd=" + cmd.GetType().Name + " canExec=" + cmd.CanExecute(null));
                            };
                        }
                    }
                }
                DiagLog.Log("WIRE done, buttons=" + _wired.Count);
            }
            catch (Exception ex)
            {
                DiagLog.Log("WIRE failed: " + ex.Message);
            }
        }

        private bool _rxTestDone;

        // TEMPORARY diagnostics: executes two throwaway ReactiveCommands in-process.
        // If these log "ran", the Rx pipeline + scheduler are fine and the problem is
        // that WPF never calls ICommand.Execute on the real buttons.
        private void RunRxPipelineTest()
        {
            if (_rxTestDone) return;
            _rxTestDone = true;

            try
            {
                var sync = ReactiveUI.ReactiveCommand.Create(() => DiagLog.Log("TEST-RX-SYNC: func ran"));
                DiagLog.Log("TEST-RX-SYNC: canExec=" + ((System.Windows.Input.ICommand)sync).CanExecute(null));
                ((System.Windows.Input.ICommand)sync).Execute(null);
                DiagLog.Log("TEST-RX-SYNC: Execute returned");
            }
            catch (Exception ex)
            {
                DiagLog.Log("TEST-RX-SYNC threw: " + ex);
            }

            try
            {
                var async = ReactiveUI.ReactiveCommand.CreateFromTask(async () =>
                {
                    DiagLog.Log("TEST-RX-ASYNC: func entered");
                    await System.Threading.Tasks.Task.Yield();
                    DiagLog.Log("TEST-RX-ASYNC: func finished");
                });
                DiagLog.Log("TEST-RX-ASYNC: canExec=" + ((System.Windows.Input.ICommand)async).CanExecute(null));
                ((System.Windows.Input.ICommand)async).Execute(null);
                DiagLog.Log("TEST-RX-ASYNC: Execute returned");
            }
            catch (Exception ex)
            {
                DiagLog.Log("TEST-RX-ASYNC threw: " + ex);
            }

            var disp = System.Windows.Threading.Dispatcher.CurrentDispatcher;
            DiagLog.Log("DISPATCHER: hash=" + disp.GetHashCode() + " thread=" + disp.Thread.ManagedThreadId
                + " paused=" + disp.HasShutdownStarted);
            var sched = ReactiveUI.RxSchedulers.MainThreadScheduler;
            DiagLog.Log("SCHED: " + sched.GetType().FullName + " hash=" + sched.GetHashCode());

            // Heartbeats: if HB-bg logs but HB-input never does, Input priority is starved
            // and every button click (all input goes through Input priority) is dead.
            var hbBg = new System.Windows.Threading.DispatcherTimer(
                System.Windows.Threading.DispatcherPriority.Background);
            hbBg.Interval = TimeSpan.FromSeconds(3);
            int bgN = 0;
            hbBg.Tick += (s, e) => DiagLog.Log("HB-bg tick " + (++bgN));
            hbBg.Start();

            var hbInput = new System.Windows.Threading.DispatcherTimer(
                System.Windows.Threading.DispatcherPriority.Input);
            hbInput.Interval = TimeSpan.FromSeconds(3);
            int inN = 0;
            hbInput.Tick += (s, e) => DiagLog.Log("HB-input tick " + (++inN));
            hbInput.Start();

            disp.BeginInvoke(new Action(() => DiagLog.Log("INPUT-ITEM: dispatched at Input priority ran")),
                System.Windows.Threading.DispatcherPriority.Input);
            disp.BeginInvoke(new Action(() => DiagLog.Log("NORMAL-ITEM: dispatched at Normal priority ran")),
                System.Windows.Threading.DispatcherPriority.Normal);

            // UI thread CPU burn sample: a spinning flood would show rising busy time.
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var cpuThread = System.Diagnostics.Process.GetCurrentProcess();
            var t0 = cpuThread.TotalProcessorTime;
            var cpuTimer = new System.Windows.Threading.DispatcherTimer(
                System.Windows.Threading.DispatcherPriority.Background);
            cpuTimer.Interval = TimeSpan.FromSeconds(6);
            cpuTimer.Tick += (s, e) =>
            {
                DiagLog.Log("PROC-CPU: elapsed=" + sw.ElapsedMilliseconds + "ms busy="
                    + (cpuThread.TotalProcessorTime - t0).TotalMilliseconds + "ms threads=" + cpuThread.Threads.Count);
            };
            cpuTimer.Start();
        }

        // TEMPORARY diagnostics: reports every Button's Command binding state.
        private void DumpButtons(string scope)
        {
            try
            {
                int n = 0;
                var queue = new System.Collections.Generic.Queue<System.Windows.DependencyObject>();
                queue.Enqueue(this);
                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    var fe = current as System.Windows.FrameworkElement;
                    var name = fe != null ? (fe.Name ?? "") : "";
                    var dc = fe != null ? (fe.DataContext == null ? "<null>" : fe.DataContext.GetType().Name) : "?";
                    DiagLog.Log("ELEM " + current.GetType().Name + " name=" + name + " dc=" + dc);

                    var btn = current as System.Windows.Controls.Button;
                    if (btn != null)
                    {
                        n++;
                        string content = "";
                        var tb = btn.Content as TextBlock;
                        if (tb != null) content = tb.Text;
                        else
                        {
                            var sp = btn.Content as StackPanel;
                            if (sp != null)
                                foreach (var child in sp.Children)
                                    if (child is TextBlock t) content += t.Text;
                        }
                        var cmd = btn.Command;
                        bool canExec = false;
                        string canErr = "";
                        if (cmd != null)
                        {
                            try { canExec = cmd.CanExecute(btn.CommandParameter); }
                            catch (System.Exception ex) { canErr = ex.GetType().Name + ":" + ex.Message; }
                        }
                        DiagLog.Log("BTN[" + n + "] '" + content + "' cmd=" + (cmd == null ? "NULL" : cmd.GetType().Name)
                            + " canExec=" + canExec + (canErr != "" ? " canErr=" + canErr : "")
                            + " isEnabled=" + btn.IsEnabled + " dc=" + (btn.DataContext == null ? "<null>" : btn.DataContext.GetType().Name));
                    }

                    int childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(current);
                    for (int i = 0; i < childCount; i++)
                    {
                        if (System.Windows.Media.VisualTreeHelper.GetChild(current, i) is DependencyObject dobj)
                            queue.Enqueue(dobj);
                    }
                }
                DiagLog.Log("DUMP complete scope=" + scope + " buttons=" + n);
            }
            catch (System.Exception ex)
            {
                DiagLog.Log("DUMP failed: " + ex);
            }
        }

    }
}
