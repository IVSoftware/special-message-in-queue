One of your comments reads,  **"I wonder if I can just solve it by designing WaitingObj.Read()**".

Let's entertain that thought by designing a `Queue` that provides some basic observability by implementing `INotifyCollectionChanged` and provides these features:

- A `ReadAsync` method to await a "special" message that matches a specified predicate.
- A `SelfTest` method that enqueues one message per second from a list of 10 messages.

An instance of `var WaitingObj = new DesignedObservableQueue()` can then be exercised in a console app to see **whether or not** this would satisfy your design specs.



    class DesignedObservableQueue : Queue<MockMessage>, INotifyCollectionChanged
    {
        public new void Enqueue(MockMessage message)
        {
            base.Enqueue(message);
            CollectionChanged?
                .Invoke(
                    this,
                    new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Add, 
                        message));
        }
        public new MockMessage Dequeue()
        {
            var message = base.Dequeue();
            CollectionChanged?
                .Invoke(
                    this,
                    new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Remove, 
                        message));
            return message;
        }
        public event NotifyCollectionChangedEventHandler? CollectionChanged;

Provide a way to detect that a special message has been enqueued.

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

Mock a queue that "is receiving messages all the time" by self-enqueuing at one-second intervals.

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
                })
            {
                Enqueue(new MockMessage { Message = message });
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }

***
Once the `TestMethod` shown in your post is changed to an `async` method, perform this simple test:

    static void Main(string[] args)
    {
        Console.Title = "Test Runner";
        var stopwatch = new Stopwatch();
        var WaitingObj = new DesignedObservableQueue();

        stopwatch.Start();
        _ = WaitingObj.SelfTest();

        // Local test method is expecting to
        // match the predicate in ~6 seconds.
        if(TestMethod().Wait(TimeSpan.FromSeconds(10)))
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

[![console output][1]][1]


  [1]: https://i.stack.imgur.com/sucS5.png