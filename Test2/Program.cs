﻿using Notan;
using Notan.Serialization;
using System;

namespace Test2
{
    struct Number : IEntity
    {
        public Handle Handle { get; set; }

        public int Value;

        public void Deserialize<T>(T deserializer) where T : IDeserializer<T>
        {
            Value = deserializer.GetEntry(nameof(Value)).ReadInt32();
        }

        public void Serialize<T>(T serializer, bool nodelta) where T : ISerializer
        {
            serializer.Write(nameof(Value), Value);
        }
    }

    struct Adder : IEntity
    {
        public Handle Handle { get; set; }

        public int Value;

        public void Deserialize<T>(T deserializer) where T : IDeserializer<T>
        {
            Value = deserializer.GetEntry(nameof(Value)).ReadInt32();
        }

        public void Serialize<T>(T serializer, bool nodelta) where T : ISerializer
        {
            serializer.Write(nameof(Value), Value);
        }
    }

    struct OuterAdderSystem : ISystem<Adder>
    {
        public Storage<Number> NumberStorage;
        public void Work(Storage<Adder> storage, ref Adder entity)
        {
            var adderSystem = new AdderSystem { Value = entity.Value };
            NumberStorage.Run(ref adderSystem);
            storage.Destroy(entity.Handle);
        }

        struct AdderSystem : ISystem<Number>
        {
            public int Value;

            public void Work(Storage<Number> storage, ref Number entity)
            {
                entity.Value += Value;
                storage.UpdateObservers(entity.Handle);
            }
        }
    }

    struct ConsoleSystem : IViewSystem<Number>
    {
        public void Work(StorageView<Number> Storage, ref Number entity)
        {
            Console.Write(entity.Value);
            Console.Write(",");
        }
    }

    struct ObserverSystem : ISystem<Number>
    {
        public Client Client;

        public void Work(Storage<Number> storage, ref Number entity)
        {
            storage.AddObserver(entity.Handle, Client);
        }
    }

    class Program
    {
        static void Main()
        {
            Console.WriteLine("Server or client? s/c");
            var c = Console.ReadKey(true).KeyChar;
            Console.WriteLine();
            if (c == 's')
            {
                ServerProcess();
            }
            else if (c == 'c')
            {
                ClientProcess();
            }
            Console.WriteLine("Halting.");
        }

        private static void SetupWorld(World world)
        {
            world.AddStorage<Number>();
            world.AddStorage<Adder>(ClientAuthority.Unauthenticated);
        }

        static void ServerProcess()
        {
            var world = new ServerWorld(4242)
            {
                Timestep = TimeSpan.FromSeconds(1)
            };
            SetupWorld(world);

            var adderStorage = world.GetStorage<Adder>();
            var numberStorage = world.GetStorage<Number>();
            numberStorage.Create().Value = 2;
            var del = numberStorage.Create();
            del.Value = 4;
            numberStorage.Create().Value = 8;
            numberStorage.Destroy(del.Handle);
            while (world.Loop())
            {
                while (world.TryDequeueClient(out var client))
                {
                    var observerSystem = new ObserverSystem { Client = client };
                    numberStorage.Run(ref observerSystem);
                }
                var outerAdderSystem = new OuterAdderSystem { NumberStorage = numberStorage };
                adderStorage.Run(ref outerAdderSystem);
            }
        }

        static void ClientProcess()
        {
            var world = ClientWorld.StartAsync("localhost", 4242).Result;
            SetupWorld(world);

            while (world.Loop())
            {
                var consoleSystem = new ConsoleSystem();
                world.GetStorageView<Number>().Run(ref consoleSystem);
                Console.WriteLine();
                Console.WriteLine("Give number:");
                world.GetStorageView<Adder>().RequestCreate(new Adder { Value = int.Parse(Console.ReadLine()) });
                Console.WriteLine();
            }
        }
    }
}
