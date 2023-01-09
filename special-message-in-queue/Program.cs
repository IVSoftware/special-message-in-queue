using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;

namespace special_message_in_queue
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Test Runner";
            var stopwatch = new Stopwatch();
            var WaitingObj = new DesignedObservableQueue();

            stopwatch.Start();
            _ = WaitingObj.SelfTest();

            // Local test method is expecting to
            // match the predicate in ~6 seconds.
            if(TestMethod().Wait(TimeSpan.FromSeconds(4)))
            {
                Console.WriteLine($"PASSED {stopwatch.Elapsed}");
            }
            else
            {
                Console.WriteLine($"FAILED {stopwatch.Elapsed}");
            }
            // Local method
            async Task TestMethod()
            {
                // do something
                await WaitingObj.ReadAsync((message) => message.Message == "special");
                // continue to do something
            }
            Console.ReadKey();
        }
    }
    class DesignedObservableQueue : Queue<MockMessage>, INotifyCollectionChanged
    {
        public new void Enqueue(MockMessage message)
        {
            base.Enqueue(message);
            CollectionChanged?
                .Invoke(
                    this,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, message));
        }
        public new MockMessage Dequeue()
        {
            var message = base.Dequeue();
            CollectionChanged?
                .Invoke(
                    this,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, message));
            return message;
        }
        public event NotifyCollectionChangedEventHandler? CollectionChanged;
        public async Task ReadAsync(Predicate<MockMessage> condition)
        {
            var awaiter = new SemaphoreSlim(0, 1);
            try
            {
                CollectionChanged += localOnCollectionChanged;
                await awaiter.WaitAsync();
            }
            finally
            {
                awaiter.Release();
                CollectionChanged -= localOnCollectionChanged;
            }

            void localOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        var message = e.NewItems!.Cast<MockMessage>().First();
                        if(condition(message))
                        {
                            Console.WriteLine($"MATCH: {message.Message}");
                            awaiter.Release();
                        }
                        else
                        {
                            Console.WriteLine($"NO MATCH: {message.Message}");
                        }
                        break;
                    default:
                        break;
                }
            }
        }
        // Enqueue one message per second.
        public async Task SelfTest()
        {
            foreach (
                var message in new[]
                {
                    "occasion",
                    "twin",
                    "intention",
                    "arrow",
                    "draw",
                    "forest",
                    "special",
                    "please",
                    "shell",
                    "momentum",
                    "coalition",
                })
            {
                Enqueue(new MockMessage { Message = message });
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }
    class MockMessage
    {
        public string Message { get; set; } = string.Empty;
    }
}