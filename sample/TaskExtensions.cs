using System.Linq;

namespace System.Threading.Tasks
{
    public static class TaskExtensions
    {
        public static Task Then(this Task task, Action next, TaskScheduler scheduler = null)
        {
            if (task == null) throw new ArgumentNullException("task");
            if (next == null) throw new ArgumentNullException("next");
            scheduler = scheduler ?? TaskScheduler.Default;

            var tcs = new TaskCompletionSource<object>();
            task.ContinueWith(delegate
                              {
                if (task.IsFaulted) tcs.TrySetException(task.Exception.InnerExceptions);
                else if (task.IsCanceled) tcs.TrySetCanceled();
                else
                {
                    try
                    {
                        next();
                        tcs.TrySetResult(null);
                    }
                    catch (Exception exc) { tcs.TrySetException(exc); }
                }
            }, scheduler);
            return tcs.Task;
        }

        public static Task<TResult> Then<TResult>(this Task task, Func<TResult> next, TaskScheduler scheduler = null)
        {
            if (task == null) throw new ArgumentNullException("task");
            if (next == null) throw new ArgumentNullException("next");
            scheduler = scheduler ?? TaskScheduler.Default;

            var tcs = new TaskCompletionSource<TResult>();
            task.ContinueWith(delegate
                              {
                if (task.IsFaulted) tcs.TrySetException(task.Exception.InnerExceptions);
                else if (task.IsCanceled) tcs.TrySetCanceled();
                else
                {
                    try
                    {
                        var result = next();
                        tcs.TrySetResult(result);
                    }
                    catch (Exception exc) { tcs.TrySetException(exc); }
                }
            }, scheduler);
            return tcs.Task;
        }

        public static Task Then<TResult>(this Task<TResult> task, Action<TResult> next, TaskScheduler scheduler = null)
        {
            if (task == null) throw new ArgumentNullException("task");
            if (next == null) throw new ArgumentNullException("next");
            scheduler = scheduler ?? TaskScheduler.Default;

            var tcs = new TaskCompletionSource<object>();
            task.ContinueWith(delegate
                              {
                if (task.IsFaulted) tcs.TrySetException(task.Exception.InnerExceptions);
                else if (task.IsCanceled) tcs.TrySetCanceled();
                else
                {
                    try
                    {
                        next(task.Result);
                        tcs.TrySetResult(null);
                    }
                    catch (Exception exc) { tcs.TrySetException(exc); }
                }
            }, scheduler);
            return tcs.Task;
        }
    }
}

