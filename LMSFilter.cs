//==============================================================
//Written by: Philip A Covington, N8VB
//
//This software is licensed under the GNU General public License
//==============================================================
//LMSFilter.cs
//LMS filtering for NR and ANF modes
//
//==============================================================

using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace SharpDSP
{
	[Serializable()]
    public struct LMSFilter
    {        
        #region Private members

        private LMSFilterType filter_type;
        private int size;
        private int lms_size;
        private int delay_line_index;
        private int mask;
        private float[] delay_line;
        private float[] coefficients;
        
        #endregion

        #region Properties

        private float adaptation_rate;
        public float AdaptationRate
        {
            get { return this.adaptation_rate; }
            set
            {
                adaptation_rate = value;
            }
        }
        private float leakage;
        public float Leakage
        {
            get { return leakage; }
            set
            {
                leakage = value;
            }
        }
        
        private int adaptive_filter_size;
        public int AdaptiveFilterSize
        {
            get { return adaptive_filter_size; }
            set
            {
                adaptive_filter_size = value;
                this.coefficients = new float[128];
            }
        }
        private int delay;
        public int Delay
        {
            get { return delay; }
            set
            {
                delay = value;
            }
        }

        #endregion

        #region Constructor

        public LMSFilter(int delay, float adaptation_rate, float leakage, int adaptive_filter_size, LMSFilterType filter_type, int size)
        {
            this.filter_type = filter_type;
            this.size = size;
            this.delay = delay;
            this.lms_size = 512;
            this.mask = this.lms_size - 1;
            this.delay_line = new float[this.lms_size];
            this.adaptation_rate = adaptation_rate;
            this.leakage = leakage;
            this.adaptive_filter_size = adaptive_filter_size;
            this.coefficients = new float[128];
            this.delay_line_index = 0;
        }

        #endregion

        #region Public Methods

        unsafe public void DoLMSFilter(float* real)
        {
            switch (this.filter_type)
            {
                case LMSFilterType.Interference:
                    DoLMSInterferenceFilter(real);
                    break;
                case LMSFilterType.Noise:
                    DoLMSNoiseFilter(real);
                    break;
            }
        }

        #endregion

        #region Private Methods

        unsafe private void DoLMSInterferenceFilter(float* real_in)
        {
            float scl1 = 1.0F - this.adaptation_rate * this.leakage;

            for (int i = 0; i < this.size; i++)
            {

                this.delay_line[this.delay_line_index] = real_in[i];
                
                float accum = 0.0F;
                float sum_sq = 0.0F;
                int k = 0;

                for (int j = 0; j < this.adaptive_filter_size; j++)
                {
                    k = (j + this.delay + this.delay_line_index) & this.mask;
                    sum_sq += this.delay_line[k] * this.delay_line[k];
                    accum += this.coefficients[j] * this.delay_line[k];
                }

                float error = real_in[i] - accum;
                real_in[i] = error; //this makes it an interference filter

                float scl2 = this.adaptation_rate / (sum_sq + 1e-10F);
                error *= scl2;

                for (int j = 0; j < this.adaptive_filter_size; j++)
                {
                    k = (j + this.delay + this.delay_line_index) & this.mask;
                    this.coefficients[j] = this.coefficients[j] * scl1 + error * this.delay_line[k];
                }

                this.delay_line_index = (this.delay_line_index + this.mask) & this.mask;
            }
        }

        unsafe private void DoLMSNoiseFilter(float* real_in)
        {
            float scl1 = 1.0F - this.adaptation_rate * this.leakage;

            for (int i = 0; i < this.size; i++)
            {

                this.delay_line[this.delay_line_index] = real_in[i];

                float accum = 0.0F;
                float sum_sq = 0.0F;
                int k = 0;

                for (int j = 0; j < this.adaptive_filter_size; j++)
                {
                    k = (j + this.delay + this.delay_line_index) & this.mask;
                    sum_sq += this.delay_line[k] * this.delay_line[k];
                    accum += this.coefficients[j] * this.delay_line[k];
                }

                float error = real_in[i] - accum;
                real_in[i] = accum;  //this makes it a noise filter

                float scl2 = this.adaptation_rate / (sum_sq + 1e-10F);
                error *= scl2;

                for (int j = 0; j < this.adaptive_filter_size; j++)
                {
                    k = (j + this.delay + this.delay_line_index) & this.mask;
                    this.coefficients[j] = this.coefficients[j] * scl1 + error * this.delay_line[k];
                }

                this.delay_line_index = (this.delay_line_index + this.mask) & this.mask;				
            }			
        }

        #endregion

    }

}