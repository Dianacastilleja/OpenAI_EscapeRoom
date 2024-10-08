using System.Collections.Generic;
using Unity.Sentis;
using NUnit.Framework;
using UnityEngine;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Inference;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Utils.Tests;

namespace Unity.MLAgents.Tests
{
    [TestFixture]
    public class EditModeTestInternalBrainTensorGenerator
    {
        [SetUp]
        public void SetUp()
        {
            if (Academy.IsInitialized)
            {
                Academy.Instance.Dispose();
            }
        }

        static List<TestAgent> GetFakeAgents(ObservableAttributeOptions observableAttributeOptions = ObservableAttributeOptions.Ignore)
        {
            var goA = new GameObject("goA");
            var bpA = goA.AddComponent<BehaviorParameters>();
            bpA.BrainParameters.VectorObservationSize = 3;
            bpA.BrainParameters.NumStackedVectorObservations = 1;
            bpA.ObservableAttributeHandling = observableAttributeOptions;
            var agentA = goA.AddComponent<TestAgent>();

            var goB = new GameObject("goB");
            var bpB = goB.AddComponent<BehaviorParameters>();
            bpB.BrainParameters.VectorObservationSize = 3;
            bpB.BrainParameters.NumStackedVectorObservations = 1;
            bpB.ObservableAttributeHandling = observableAttributeOptions;
            var agentB = goB.AddComponent<TestAgent>();

            var agents = new List<TestAgent> { agentA, agentB };
            foreach (var agent in agents)
            {
                agent.LazyInitialize();
            }
            agentA.collectObservationsSensor.AddObservation(new Vector3(1, 2, 3));
            agentB.collectObservationsSensor.AddObservation(new Vector3(4, 5, 6));

            var infoA = new AgentInfo
            {
                storedActions = new ActionBuffers(null, new[] { 1, 2 }),
                discreteActionMasks = null,
            };

            var infoB = new AgentInfo
            {
                storedActions = new ActionBuffers(null, new[] { 3, 4 }),
                discreteActionMasks = new[] { true, false, false, false, false },
            };


            agentA._Info = infoA;
            agentB._Info = infoB;
            return agents;
        }

        [Test]
        public void Construction()
        {
            var mem = new Dictionary<int, List<float>>();
            var tensorGenerator = new TensorGenerator(0, mem);
            Assert.IsNotNull(tensorGenerator);
        }

        [Test]
        public void GenerateBatchSize()
        {
            var inputTensor = new TensorProxy();
            const int batchSize = 4;
            var generator = new BatchSizeGenerator();
            generator.Generate(inputTensor, batchSize, null);
            Assert.IsNotNull(inputTensor.data);
            Assert.AreEqual(((Tensor<int>)inputTensor.data)[0], batchSize);
        }

        [Test]
        public void GenerateSequenceLength()
        {
            var inputTensor = new TensorProxy();
            const int batchSize = 4;
            var generator = new SequenceLengthGenerator();
            generator.Generate(inputTensor, batchSize, null);
            Assert.IsNotNull(inputTensor.data);
            Assert.AreEqual(((Tensor<int>)inputTensor.data)[0], 1);
        }

        [Test]
        public void GenerateVectorObservation()
        {
            var inputTensor = new TensorProxy
            {
                valueType = TensorProxy.TensorType.FloatingPoint,
                shape = new int[] { 2, 4 }
            };
            const int batchSize = 4;
            var agentInfos = GetFakeAgents(ObservableAttributeOptions.ExamineAll);
            var generator = new ObservationGenerator();
            generator.AddSensorIndex(0); // ObservableAttribute (size 1)
            generator.AddSensorIndex(1); // TestSensor (size 0)
            generator.AddSensorIndex(2); // TestSensor (size 0)
            generator.AddSensorIndex(3); // VectorSensor (size 3)
            var agent0 = agentInfos[0];
            var agent1 = agentInfos[1];
            var inputs = new List<AgentInfoSensorsPair>
            {
                new AgentInfoSensorsPair { agentInfo = agent0._Info, sensors = agent0.sensors },
                new AgentInfoSensorsPair { agentInfo = agent1._Info, sensors = agent1.sensors },
            };
            generator.Generate(inputTensor, batchSize, inputs);
            Assert.IsNotNull(inputTensor.data);
            Assert.AreEqual((int)((Tensor<float>)inputTensor.data)[0, 1], 1);
            Assert.AreEqual((int)((Tensor<float>)inputTensor.data)[0, 3], 3);
            Assert.AreEqual((int)((Tensor<float>)inputTensor.data)[1, 1], 4);
            Assert.AreEqual((int)((Tensor<float>)inputTensor.data)[1, 3], 6);
        }

        [Test]
        public void GeneratePreviousActionInput()
        {
            var inputTensor = new TensorProxy
            {
                shape = new int[] { 2, 2 },
                valueType = TensorProxy.TensorType.Integer
            };
            const int batchSize = 4;
            var agentInfos = GetFakeAgents();
            var generator = new PreviousActionInputGenerator();
            var agent0 = agentInfos[0];
            var agent1 = agentInfos[1];
            var inputs = new List<AgentInfoSensorsPair>
            {
                new AgentInfoSensorsPair { agentInfo = agent0._Info, sensors = agent0.sensors },
                new AgentInfoSensorsPair { agentInfo = agent1._Info, sensors = agent1.sensors },
            };
            generator.Generate(inputTensor, batchSize, inputs);
            Assert.IsNotNull(inputTensor.data);
            Assert.AreEqual(((Tensor<int>)inputTensor.data)[0, 0], 1);
            Assert.AreEqual(((Tensor<int>)inputTensor.data)[0, 1], 2);
            Assert.AreEqual(((Tensor<int>)inputTensor.data)[1, 0], 3);
            Assert.AreEqual(((Tensor<int>)inputTensor.data)[1, 1], 4);
        }

        [Test]
        public void GenerateActionMaskInput()
        {
            var inputTensor = new TensorProxy
            {
                shape = new int[] { 2, 5 },
                valueType = TensorProxy.TensorType.FloatingPoint
            };
            const int batchSize = 4;
            var agentInfos = GetFakeAgents();
            var generator = new ActionMaskInputGenerator();

            var agent0 = agentInfos[0];
            var agent1 = agentInfos[1];
            var inputs = new List<AgentInfoSensorsPair>
            {
                new AgentInfoSensorsPair { agentInfo = agent0._Info, sensors = agent0.sensors },
                new AgentInfoSensorsPair { agentInfo = agent1._Info, sensors = agent1.sensors },
            };

            generator.Generate(inputTensor, batchSize, inputs);
            Assert.IsNotNull(inputTensor.data);
            Assert.AreEqual((int)((Tensor<float>)inputTensor.data)[0, 0], 1);
            Assert.AreEqual((int)((Tensor<float>)inputTensor.data)[0, 4], 1);
            Assert.AreEqual((int)((Tensor<float>)inputTensor.data)[1, 0], 0);
            Assert.AreEqual((int)((Tensor<float>)inputTensor.data)[1, 4], 1);
        }
    }
}
