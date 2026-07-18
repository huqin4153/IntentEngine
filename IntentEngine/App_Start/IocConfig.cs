using System;
using System.Collections.Generic;
using IntentEngine.Contracts;
using IntentEngine.Repositories;
using IntentEngine.Services;
using IntentEngine.DataExecutors;

namespace IntentEngine
{
    public class ServiceContainer
    {
        private readonly Dictionary<Type, object> _singletons = new Dictionary<Type, object>();

        public void RegisterSingleton<T>(T instance) where T : class
        {
            _singletons[typeof(T)] = instance;
        }

        public T Get<T>() where T : class
        {
            if (_singletons.TryGetValue(typeof(T), out var instance))
                return instance as T;

            var type = typeof(T);
            if (type.IsInterface || type.IsAbstract)
                throw new InvalidOperationException(
                    $"类型 {type.Name} 未在 IoC 容器中注册。请确保 IocConfig.Initialize() 已执行。");

            var impl = Activator.CreateInstance<T>();
            _singletons[type] = impl;
            return impl;
        }

        public IEmbeddingService GetEmbeddingService() => Get<IEmbeddingService>();
        public IIntentMatcher GetIntentMatcher() => Get<IIntentMatcher>();
        public IFlowEngine GetFlowEngine() => Get<IFlowEngine>();
        public IIntentRepository GetIntentRepo() => Get<IIntentRepository>();
        public IFunctionRepository GetFunctionRepo() => Get<IFunctionRepository>();
        public IFlowStepRepository GetFlowStepRepo() => Get<IFlowStepRepository>();
        public IFunctionParameterRepository GetParamRepo() => Get<IFunctionParameterRepository>();
        public IConfigService GetConfigService() => Get<IConfigService>();
        public IDataExecutorFactory GetExecutorFactory() => Get<IDataExecutorFactory>();
    }

    public static class IocConfig
    {
        public static ServiceContainer Container { get; private set; }
        public static string OnnxInitError { get; set; }

        public static void Initialize()
        {
            Container = new ServiceContainer();

            Container.RegisterSingleton<IIntentRepository>(new IntentRepository());
            Container.RegisterSingleton<IFunctionRepository>(new FunctionRepository());
            Container.RegisterSingleton<IFlowStepRepository>(new FlowStepRepository());
            Container.RegisterSingleton<IFunctionParameterRepository>(new FunctionParameterRepository());

            IEmbeddingService embedding = null;
            try
            {
                var onnx = new OnnxEmbeddingService();
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string modelPath = System.IO.Path.Combine(baseDir, "Resources", "bge-small-zh-v1.5.onnx");
                string vocabPath = System.IO.Path.Combine(baseDir, "Resources", "vocab.txt");
                onnx.Load(modelPath, vocabPath);
                if (onnx.IsReady) { embedding = onnx; }
                else { OnnxInitError = onnx.LastError ?? "Load返回false"; }
            }
            catch (System.Exception ex) { OnnxInitError = ex.ToString(); }
            if (embedding == null) embedding = new CharEmbeddingService();
            Container.RegisterSingleton<IEmbeddingService>(embedding);

            Container.RegisterSingleton<IIntentMatcher>(new DefaultIntentMatcher(
                Container.Get<IEmbeddingService>(),
                Container.Get<IIntentRepository>()
            ));

            var executorFactory = new DataExecutorFactory();
            executorFactory.Register("SQLite", cs => new SqliteDataExecutor(cs));
            executorFactory.Register("SqlServer", cs => new SqlServerDataExecutor(cs));
            executorFactory.Register("Oracle", cs => new OracleDataExecutor(cs));
            executorFactory.Register("MySQL", cs => new MySqlDataExecutor(cs));
            Container.RegisterSingleton<IDataExecutorFactory>(executorFactory);

            Container.RegisterSingleton<IFlowEngine>(new DefaultFlowEngine(
                Container.Get<IFlowStepRepository>(),
                Container.Get<IFunctionParameterRepository>(),
                executorFactory,
                Container.Get<IFunctionRepository>()
            ));

            Container.RegisterSingleton<IConfigService>(new DefaultConfigService(
                Container.Get<IIntentRepository>(),
                Container.Get<IFunctionRepository>(),
                Container.Get<IFlowStepRepository>(),
                Container.Get<IFunctionParameterRepository>(),
                Container.Get<IEmbeddingService>(),
                Container.Get<IIntentMatcher>()
            ));

        }
    }
}
