using System;
using System.Collections.Generic;
using System.Text;
using Trx.Utilities;
using System.IO.IsolatedStorage;
using System.Runtime.Serialization.Formatters.Binary;

namespace PinIssuance.Net.Bridge.PostBridge.Utilities
{
    public class IsolatedStanSequencer : ISequencer
    {
        IsolatedStorageFile appFile = IsolatedStorageFile.GetMachineStoreForAssembly();
        private StanSequence sequence;

        public IsolatedStanSequencer()
        {
            IsolatedStorageFileStream fileStream = new IsolatedStorageFileStream("isoClient.ser", System.IO.FileMode.OpenOrCreate, appFile);
            byte[] buffer = new byte[fileStream.Length];
            fileStream.Read(buffer, 0, buffer.Length);
            fileStream.Close();

            sequence = DeSerializeObject(buffer) as StanSequence;
            if (sequence == null)
            {
                sequence = new StanSequence();
            }
        }

        private Object DeSerializeObject(Byte[] serializedObject)
        {
            Object deSerializedObject = null;
            if (serializedObject != null)
            {
                System.IO.MemoryStream ms = new System.IO.MemoryStream(serializedObject);
                try
                {
                    deSerializedObject = new BinaryFormatter().Deserialize(ms);
                }
                catch
                {
                }
                finally
                {
                    ms.Close();
                }
            }
            return deSerializedObject;
        }

        private Byte[] SerializeObject(Object obj)
        {
            Byte[] serializedObject = null;
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.TypeFormat = System.Runtime.Serialization.Formatters.FormatterTypeStyle.TypesAlways;
            bf.AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Full;
            bf.Serialize(ms, obj);
            serializedObject = new Byte[ms.Length];
            ms.Position = 0;
            ms.Read(serializedObject, 0, (int)ms.Length);
            ms.Close();
            return serializedObject;
        }
        #region ISequencer Members

        public int CurrentValue()
        {
            return sequence.SequencerCurrentValue;
        }

        public int Increment()
        {
            sequence.SequencerCurrentValue += sequence.SequencerIncrementValue;
            if (sequence.SequencerCurrentValue > sequence.SequencerMaxValue)
            {
                sequence.SequencerCurrentValue = sequence.SequencerMinValue;
            }

            byte[] buffer = SerializeObject(sequence);
            IsolatedStorageFileStream fileStream = new IsolatedStorageFileStream("isoClient.ser", System.IO.FileMode.OpenOrCreate, appFile);
            fileStream.Write(buffer,0, buffer.Length);
            fileStream.Close();
            
            return sequence.SequencerCurrentValue;
        }

        public int Maximum()
        {
            
            return sequence.SequencerMaxValue;
        }

        public int Minimum()
        {
            return sequence.SequencerMinValue;
        }

        #endregion
    }
}
