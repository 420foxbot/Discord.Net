﻿using System;
using System.Threading.Tasks;

namespace Discord.Extensions
{
    internal static class EventExtensions
    {
        //TODO: Optimize these for if there is only 1 subscriber (can we do this?)
        //TODO: Could we maintain our own list instead of generating one on every invocation?
        public static async Task RaiseAsync(this Func<Task> eventHandler)
        {
            var subscriptions = eventHandler?.GetInvocationList();
            if (subscriptions != null)
            {
                for (int i = 0; i < subscriptions.Length; i++)
                    await (subscriptions[i] as Func<Task>).Invoke().ConfigureAwait(false);
            }
        }
        public static async Task RaiseAsync<T>(this Func<T, Task> eventHandler, T arg)
        {
            var subscriptions = eventHandler?.GetInvocationList();
            if (subscriptions != null)
            {
                for (int i = 0; i < subscriptions.Length; i++)
                    await (subscriptions[i] as Func<T, Task>).Invoke(arg).ConfigureAwait(false);
            }
        }
        public static async Task RaiseAsync<T1, T2>(this Func<T1, T2, Task> eventHandler, T1 arg1, T2 arg2)
        {
            var subscriptions = eventHandler?.GetInvocationList();
            if (subscriptions != null)
            {
                for (int i = 0; i < subscriptions.Length; i++)
                    await (subscriptions[i] as Func<T1, T2, Task>).Invoke(arg1, arg2).ConfigureAwait(false);
            }
        }
        public static async Task RaiseAsync<T1, T2, T3>(this Func<T1, T2, T3, Task> eventHandler, T1 arg1, T2 arg2, T3 arg3)
        {
            var subscriptions = eventHandler?.GetInvocationList();
            if (subscriptions != null)
            {
                for (int i = 0; i < subscriptions.Length; i++)
                    await (subscriptions[i] as Func<T1, T2, T3, Task>).Invoke(arg1, arg2, arg3).ConfigureAwait(false);
            }
        }
        public static async Task RaiseAsync<T1, T2, T3, T4>(this Func<T1, T2, T3, T4, Task> eventHandler, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            var subscriptions = eventHandler?.GetInvocationList();
            if (subscriptions != null)
            {
                for (int i = 0; i < subscriptions.Length; i++)
                    await (subscriptions[i] as Func<T1, T2, T3, T4, Task>).Invoke(arg1, arg2, arg3, arg4).ConfigureAwait(false);
            }
        }
    }
}
