using System;
using System.Collections.Generic;
using System.Text;

namespace PinIssuance.Net.Bridge.PostBridge.Utilities
{
    [Serializable]
    internal class StanSequence
    {
        private int _SequencerIncrementValue = 1;
        private int _SequencerMaxValue = 999999;
        private int _SequencerMinValue = 1;
        private int _SequencerCurrentValue = 1;



         public int SequencerIncrementValue
        {
            get
            {
                return _SequencerIncrementValue;
            }
            set
            {
                _SequencerIncrementValue = value;
            }
        }

        public int SequencerMaxValue
        {
            get
            {
                return _SequencerMaxValue;
            }
            set
            {
                _SequencerMaxValue= value;
            }
        }

        public int SequencerMinValue
        {
            get
            {
                return _SequencerMinValue;
            }
            set
            {
                _SequencerMinValue= value;
            }
        }

        public int SequencerCurrentValue
        {
            get
            {
                return _SequencerCurrentValue;
            }
            set
            {
                _SequencerCurrentValue = value;
            }
        }
    }

}

       
    
