using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WiiTUIO.Provider
{
    class DuoTouch
    {
        private SmoothingBuffer smoothingBuffer;

        private Vector screenSize;

        private ulong masterID = 1;
        private ulong slaveID = 2;

        private WiiContact lastMasterContact;
        private WiiContact lastSlaveContact;

        private Point masterPosition;
        private Point slavePosition;

        private bool masterHovering = false;

        private bool releaseMaster = false;
        private bool masterReleased = true;
        private bool releaseSlave = false;
        private bool slaveReleased = true;

        private bool hoverDisabled = false;

        public DuoTouch(Vector screenSize, int smoothSize)
        {
            this.screenSize = screenSize;
            this.smoothingBuffer = new SmoothingBuffer(smoothSize);
        }

        public void setMasterPosition(Point position)
        {
            this.masterPosition.X = position.X;
            this.masterPosition.Y = position.Y;
        }

        public void setSlavePosition(Point position)
        {
            this.slavePosition.X = position.X;
            this.slavePosition.Y = position.Y;
        }

        public void setContactMaster()
        {
            this.masterReleased = false;
        }

        public void setContactSlave()
        {
            this.slaveReleased = false;
        }

        public void releaseContactMaster()
        {
            if (!this.masterReleased)
            {
                this.releaseMaster = true;
            }
        }

        public void releaseContactSlave()
        {
            if (!this.slaveReleased)
            {
                this.releaseSlave = true;
            }
        }

        public void disableHover()
        {
            this.hoverDisabled = true;
        }

        public void enableHover()
        {
            this.hoverDisabled = false;
        }

        public Queue<WiiContact> getFrame()
        {
            Queue<WiiContact> newFrame = new Queue<WiiContact>(2);

            //master
            if(masterPosition != null)
            {
                ContactType contactType;
                Point position = new Point();

                if (this.releaseMaster)
                {
                    contactType = ContactType.End;
                    position = lastMasterContact.Position;
                    this.masterHovering = true;
                    this.masterReleased = true;
                    this.releaseMaster = false;
                }
                else if (!this.masterReleased)
                {
                    if (this.masterHovering)
                    {
                        contactType = ContactType.Start;
                        this.masterHovering = false;
                    }
                    else
                    {
                        contactType = ContactType.Move;
                    }
                    smoothingBuffer.addValue(new Vector(masterPosition.X, masterPosition.Y));
                    Vector smoothedVec = smoothingBuffer.getSmoothedValue();
                    position.X = smoothedVec.X;
                    position.Y = smoothedVec.Y;
                }
                else //Released = hovering
                {
                    contactType = ContactType.Hover;

                    if (!this.hoverDisabled)
                    {
                        smoothingBuffer.addValue(new Vector(masterPosition.X, masterPosition.Y));
                        Vector smoothedVec = smoothingBuffer.getSmoothedValue();
                        position.X = smoothedVec.X;
                        position.Y = smoothedVec.Y;
                    }

                    this.masterHovering = true;
                }

                if (!(contactType == ContactType.Hover && this.hoverDisabled))
                {
                    this.lastMasterContact = new WiiContact(this.masterID, contactType, position, this.screenSize);
                    newFrame.Enqueue(this.lastMasterContact);
                }
            }


            return newFrame;
        }

    }
}
