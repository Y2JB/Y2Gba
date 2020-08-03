using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gba.Core
{
    public class Scheduler
    {
        GameboyAdvance gba;

        IScheudledItem[] scheduledItems;
        
        UInt32 nextScheduledEvent;
        int scheduleItem;

        public Scheduler(GameboyAdvance gba)
        {
            this.gba = gba;

            scheduledItems = new IScheudledItem[6];
            scheduledItems[0] = gba.LcdController;
            scheduledItems[1] = gba.Timers;
            scheduledItems[2] = gba.Dma[0];
            scheduledItems[3] = gba.Dma[1];
            scheduledItems[4] = gba.Dma[2];
            scheduledItems[5] = gba.Dma[3];
        }


        public void Check()  
        {
            if(gba.Cpu.Cycles >= nextScheduledEvent)
            {
                scheduledItems[scheduleItem].ScheduledUpdate();
                RefreshSchedule();

                // Check there isn't another event scheduled for this cycle
                Check();
            }            
        }
            

        public void RefreshSchedule()
        {
            UInt32 nextUpdate = 0xFFFFFFFF;
            UInt32 cpuCycle = gba.Cpu.Cycles;

            // Go though are schedulable (is that a word?) items and figue out which will want attention next
            for(int i=0; i < scheduledItems.Length; i++)          
            {
             
                UInt32 eventCycle = scheduledItems[i].ScheduledUpdateOnCycle;
                if (eventCycle < nextUpdate)
                {
                    nextScheduledEvent = eventCycle;
                    scheduleItem = i;

                    nextUpdate = eventCycle;
                }
            }
        }
    }


    public interface IScheudledItem
    {
        void ScheduledUpdate();
        UInt32 ScheduledUpdateOnCycle { get; }
    }
}
