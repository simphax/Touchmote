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

        private bool stepIDs = false;

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

        private bool slaveEnded = false;

        private bool masterReleased = true;
        private bool slaveReleased = true;

        private bool hoverDisabled = false;


        private bool isFirstMasterContact = true;
        private Point firstMasterContact;
        private bool masterHoldPosition = true;
        public double TouchHoldThreshold = 30.0;

        public double EdgeHelperMargins = 30.0;
        public double EdgeHelperRelease = 60.0;


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

                    
                    
                    if (this.isFirstMasterContact)
                    {
                        this.firstMasterContact = this.masterPosition;
                    }
                    else 
                    {
                        if (this.masterHoldPosition)
                        {
                            if (Math.Abs(this.firstMasterContact.X - this.masterPosition.X) < TouchHoldThreshold && Math.Abs(this.firstMasterContact.Y - this.masterPosition.Y) < TouchHoldThreshold)
                            {
                                /*Console.WriteLine("DiffX: " + Math.Abs(this.firstMasterContact.X - this.masterPosition.X) + " DiffY: " + Math.Abs(this.firstMasterContact.Y - this.masterPosition.Y));*/
                                this.masterPosition = this.firstMasterContact;
                                this.masterHoldPosition = true;
                            }
                            else
                            {
                                this.masterHoldPosition = false;
                            }
                        }

                        //Helps to perform "edge swipe" guestures
                        if (this.firstMasterContact.X < EdgeHelperMargins && this.masterPosition.X < EdgeHelperRelease) //Left
                        {
                            this.masterPosition.Y = (this.firstMasterContact.Y + this.firstMasterContact.Y + this.masterPosition.Y) / 3;
                        }
                        if (this.firstMasterContact.X > (this.screenSize.X - EdgeHelperMargins) && this.masterPosition.X > (this.screenSize.X - EdgeHelperRelease)) //Right
                        {
                            this.masterPosition.Y = (this.firstMasterContact.Y + this.firstMasterContact.Y + this.masterPosition.Y) / 3;
                        }
                        if (this.firstMasterContact.Y < EdgeHelperMargins && this.masterPosition.Y < EdgeHelperRelease) //Top
                        {
                            this.masterPosition.X = (this.firstMasterContact.X + this.firstMasterContact.X + this.masterPosition.X) / 3;
                        }
                        if (this.firstMasterContact.Y > (this.screenSize.Y - EdgeHelperMargins) && this.masterPosition.Y > (this.screenSize.Y - EdgeHelperRelease)) //Bottom
                        {
                            this.masterPosition.X = (this.firstMasterContact.X + this.firstMasterContact.X + this.masterPosition.X) / 3;
                        }
                    }

                    smoothingBuffer.addValue(new Vector(masterPosition.X, masterPosition.Y));
                    Vector smoothedVec = smoothingBuffer.getSmoothedValue();
                    this.masterPosition.X = smoothedVec.X;
                    this.masterPosition.Y = smoothedVec.Y;

                    this.isFirstMasterContact = false;
                    
                }
                else //Released = hovering
                {
                    if (!this.masterHovering) //End the touch first
                    {
                        contactType = ContactType.EndToHover;
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

                    this.isFirstMasterContact = true;
                    this.masterHoldPosition = true;
                }

                if (!(contactType == ContactType.Hover && this.hoverDisabled))
                {
                    if (this.stepIDs && contactType == ContactType.EndToHover) //If we release slave touch before we release master touch we want to make sure Windows treats master as the main touch point again
                    {
                        this.lastMasterContact = new WiiContact(this.masterID, ContactType.End, this.masterPosition, this.screenSize);
                        this.masterID += 2;
                        this.slaveID += 2;
                        this.stepIDs = false;
                    }
                    else
                    {
                        this.lastMasterContact = new WiiContact(this.masterID, contactType, this.masterPosition, this.screenSize);
                    }
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

                        if (this.slavePosition.X < 0)
                        {
                            this.slavePosition.X = 0;
                        }
                        if (this.slavePosition.Y < 0)
                        {
                            this.slavePosition.Y = 0;
                        }

                        if (this.slavePosition.X > this.screenSize.X)
                        {
                            this.slavePosition.X = this.screenSize.X-1;
                        }
                        if (this.slavePosition.Y > this.screenSize.Y)
                        {
                            this.slavePosition.Y = this.screenSize.Y-1;
                        }
                    }
                    else
                    {
                        this.usingMidpoint = false;
                    }

                    this.slaveEnded = false;
                    this.stepIDs = false;
                }
                else
                {
                    if (!this.slaveHovering)
                    {
                        contactType = ContactType.EndToHover;
                        this.slavePosition = lastSlaveContact.Position;
                        this.slaveHovering = true;
                    }
                    else
                    {
                        contactType = ContactType.EndFromHover;
                    }
                }

                if (!this.slaveEnded)
                {
                    this.lastSlaveContact = new WiiContact(this.slaveID, contactType, this.slavePosition, this.screenSize);
                    newFrame.Enqueue(this.lastSlaveContact);

                    if (contactType == ContactType.EndFromHover)
                    {
                        this.slaveEnded = true;
                        if(!this.masterReleased) //If we release slave before master
                        {
                            this.stepIDs = true;
                        }
                    }
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
