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

        private Point midpoint;

        private bool usingMidpoint = false;

        private bool masterHovering = true;
        private bool slaveHovering = true;

        private bool masterReleased = true;
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
            if (this.slaveReleased) //Slave will only move with master
            {
                this.slavePosition.X = position.X;
                this.slavePosition.Y = position.Y;
            }
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
            this.masterReleased = true;
        }

        public void releaseContactSlave()
        {
            this.slaveReleased = true;
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
            Queue<WiiContact> newFrame = new Queue<WiiContact>(1);

            //master
            if(masterPosition != null)
            {
                ContactType contactType;

                if (!this.masterReleased)
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
                    this.masterPosition.X = smoothedVec.X;
                    this.masterPosition.Y = smoothedVec.Y;
                }
                else //Released = hovering
                {
                    if (!this.masterHovering) //We end it first, since we are not really doing a correct hover sequence in the provider handler. Should be fixed?
                    {
                        contactType = ContactType.End;
                        this.masterPosition = lastMasterContact.Position;
                        this.masterHovering = true;
                    }
                    else
                    {
                        contactType = ContactType.Hover;
                        smoothingBuffer.addValue(new Vector(masterPosition.X, masterPosition.Y));
                        Vector smoothedVec = smoothingBuffer.getSmoothedValue();
                        this.masterPosition.X = smoothedVec.X;
                        this.masterPosition.Y = smoothedVec.Y;
                    }

                }

                if (!(contactType == ContactType.Hover && this.hoverDisabled))
                {
                    this.lastMasterContact = new WiiContact(this.masterID, contactType, this.masterPosition, this.screenSize);
                    newFrame.Enqueue(this.lastMasterContact);
                }
            }

            //slave
            if (slavePosition != null)
            {
                ContactType contactType;

                if (!this.slaveReleased)
                {
                    if (this.slaveHovering)
                    {
                        contactType = ContactType.Start;
                        this.slaveHovering = false;
                    }
                    else
                    {
                        contactType = ContactType.Move;
                    }

                    if (!this.masterReleased)
                    {
                        if (!this.usingMidpoint)
                        {
                            this.midpoint = calculateMidpoint(this.masterPosition, this.slavePosition);
                            this.usingMidpoint = true;
                        }

                        this.slavePosition = reflectThroughMidpoint(this.masterPosition, this.midpoint);

                    }
                    else
                    {
                        this.usingMidpoint = false;
                    }

                }
                else
                {
                    if (!this.slaveHovering)
                    {
                        contactType = ContactType.End;
                        this.slavePosition = lastSlaveContact.Position;
                        this.slaveHovering = true;
                    }
                    else
                    {
                        contactType = ContactType.Hover;
                    }
                }

                if (contactType != ContactType.Hover)
                {
                    this.lastSlaveContact = new WiiContact(this.slaveID, contactType, this.slavePosition, this.screenSize);
                    newFrame.Enqueue(this.lastSlaveContact);
                }

            }

            return newFrame;
        }

        private Point calculateMidpoint(Point p1, Point p2)
        {
            return new Point((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2);
        }

        private Point reflectThroughMidpoint(Point reflect, Point basePoint)
        {
            return new Point(basePoint.X - (reflect.X - basePoint.X), basePoint.Y - (reflect.Y - basePoint.Y));
        }

    }
}
