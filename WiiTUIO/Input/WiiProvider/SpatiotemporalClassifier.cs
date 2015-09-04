
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using WiiTUIO.Properties;

namespace WiiTUIO.Provider
{
    /// <summary>
    /// A SpatiotemporalClassifier is a class which is able to classify a frame of points based on the set of previous points
    /// avaiable to it.  This is designed to solve the problem of the lack of buffer-ordering from data returned by the Wiimote.
    /// As such this has no concept of order; rather order is determined based on the likelyhood of it a point being related to another
    /// if it was in a similar place.
    /// Whilst there is no perfect solution beyond implementing buffer tracking within the actual Wiimote hardware - this class
    /// uses some spatial and temporal statistics to compute a likely solution using simple calculations.  As such, while its output
    /// may be 'good enough' it is far from perfect.
    /// </summary>
    public class SpatioTemporalClassifier
    {
        #region Helper Pair Class
        /// <summary>
        /// A ProcessPair is a simple helper class which is used to enumerate and compare trackers and inputs.
        /// </summary>
        private class ProcessPair : IComparable<ProcessPair>
        {
            /// <summary>
            /// The input in this pair.
            /// </summary>
            public SpatioTemporalInput pInput;
            /// <summary>
            /// The tracker in this pair.
            /// </summary>
            public SpatioTemporalTracker pTracker;
            /// <summary>
            /// The ranking between the tracker and the pair.
            /// </summary>
            public double fRanking;

            /// <summary>
            /// Construct a new process pair.  This will invoke 'getClassificationRanking' on the tracker.
            /// </summary>
            /// <param name="pTracker">The tracker to pair.</param>
            /// <param name="pInput">The input to pair.</param>
            public ProcessPair(SpatioTemporalTracker pTracker, SpatioTemporalInput pInput)
            {
                this.pInput = pInput;
                this.pTracker = pTracker;
                this.fRanking = pTracker.getClassificationRanking(this.pInput);
            }

            #region IComparable<ProcessPair> Members
            /// <summary>
            /// Compare this process pair to another by looking at the ranking.
            /// </summary>
            /// <param name="pOther">The other process pair to compare two.</param>
            /// <returns>A 32-Bit Signed Integer that states the relative sortorder of the two objects</returns>
            public int CompareTo(ProcessPair pOther)
            {
                return this.fRanking.CompareTo(pOther.fRanking);
            }
            #endregion
        }
        #endregion

        #region Classifier Properties
        /// <summary>
        /// A list of active trackers.
        /// </summary>
        private List<SpatioTemporalTracker> lTrackers;

        /// <summary>
        /// The default smoothing value for any trackers created.
        /// </summary>
        public int DefaultSmoothSize { get; set; }

        /// <summary>
        /// The distance at which we are confident two trackers are duplicating/following the same input.
        /// </summary>
        public double DuplicateDistance { get; set; }

        /// <summary>
        /// A delegate which describes the function signature of the events this class raises.
        /// </summary>
        /// <param name="pSource">The classifer which raised the event.</param>
        /// <param name="pTracker">The tracker of interest.</param>
        public delegate void TrackerEventHandler(SpatioTemporalClassifier pSource, SpatioTemporalTracker pTracker);

        /// <summary>
        /// A counter for session varaibles.
        /// </summary>
        private ulong iNextID = 0;

        /// <summary>
        /// An event which is raised when a new tracker is created.
        /// </summary>
        public event TrackerEventHandler OnStart;

        /// <summary>
        /// An event which is raised when an existing tracker is removed.
        /// </summary>
        public event TrackerEventHandler OnEnd;

        /// <summary>
        /// An event which is raised when an existing tracker is updated.
        /// </summary>
        public event TrackerEventHandler OnUpdate;
        #endregion

        /// <summary>
        /// Construct a new SpatioTemporalClassifier.
        /// </summary>
        public SpatioTemporalClassifier()
        {
            // Create the table.
            this.lTrackers = new List<SpatioTemporalTracker>(4);

            // Defaults.
            this.DefaultSmoothSize = 3;
            this.DuplicateDistance = 10;
        }

        /// <summary>
        /// Reset the tracker list so there is no context preserved after this call.
        /// This also resets the session counter to 0.
        /// </summary>
        public void reset()
        {
            this.lTrackers.Clear();
            this.iNextID = 0;
        }

        /// <summary>
        /// This is called to process a 'frame' of inputs which will be ranked against eachother before being dispatched
        /// to the appropriate existing inputs, removing old ones or generating new ones.
        /// </summary>
        /// <param name="tInputs">The array of inputs which we want to process as a frame.</param>
        public void processFrame(List<SpatioTemporalInput> lInputs)
        {
            // Check we have data.
            if (lInputs.Count + lTrackers.Count == 0)
                return;

            #region Ugly (but fairly quick) duplicate tracker removal.
            // Loop over an exclusive half-diagonal of a number square! :-)  Ahh the memories!
            bool bPossible = true;
            bool bFound = false;
            while (bPossible)
            {
                // Check for a collision!
                for (int i = 0; i < lTrackers.Count; ++i)
                {
                    // Flag that we have not found a collision on this loop round.
                    bFound = false;
                    for (int j = i + 1; j < lTrackers.Count; ++j)
                    {
                        // Test the trackers are in very close range of eachother (i.e. overlapping and stealing inputs!).
                        if ((lTrackers[i].Position - lTrackers[j].Position).Length < this.DuplicateDistance)
                        {
                            // They are too close - set one to be dead!
                            SpatioTemporalTracker pRemove = lTrackers[i];

                            // Remove it.
                            this.lTrackers.Remove(pRemove);
                            pRemove.eTrackerState = TrackerState.Destroy;
                            bFound = true;

                            // Raise the event.
                            if (pRemove.StrongLock)
                            {
                                if (this.OnEnd != null)
                                    this.OnEnd(this, pRemove);
                            }
                        }
                    }

                    // If we found a collision we have removed it from the list and this need to check again.
                    if (bFound)
                        break;
                }

                // So we got this far and had no collisions.
                bPossible = bFound;
            }
            #endregion

            #region Process and Sort the table of all possible trackers.
            // Build the big table of trackers-inputs-distances.
            List<ProcessPair> lTable = new List<ProcessPair>(lInputs.Count * lTrackers.Count);
            foreach (SpatioTemporalTracker pTracker in lTrackers)
            {
                foreach (SpatioTemporalInput pInput in lInputs)
                {
                    // Only add it to to the table if it will not be rejected out of hand.
                    ProcessPair pPair = new ProcessPair(pTracker, pInput);
                    //if (pPair.fRanking < pTracker.PredictionScale)
                    lTable.Add(pPair);
                }
            }

            // Sort the above table based on the distance rankings.
            lTable.Sort();
            #endregion

            #region Strip duplicate pairs from the tracker ranking table based on 'best' selections.
            // Remove duplicate tracker entries from the table whilst saving the best pairs in their own table.
            List<ProcessPair> lBest = new List<ProcessPair>();
            foreach (ProcessPair pPair in lTable)
            {
                // Does the table of best matches already contain this tracker?
                bool bContains = false;
                for (int i = 0; i < lBest.Count; ++i)
                {
                    if (lBest[i].pTracker == pPair.pTracker)
                    {
                        bContains = true;
                        break;
                    }
                }

                // If it doesn't then add it.
                if (!bContains)
                    lBest.Add(pPair);
            }
            #endregion

            #region Build a list of remove candidates.
            // Create an array which contains old trackers to be removed.
            List<SpatioTemporalTracker> lRemove = new List<SpatioTemporalTracker>();
            foreach (SpatioTemporalTracker pTracker in lTrackers)
            {
                // Does the table of best matches already contain this input?
                bool bContains = false;
                for (int i = 0; i < lBest.Count; ++i)
                {
                    if (lBest[i].pTracker == pTracker)
                    {
                        bContains = true;
                        break;
                    }
                }

                // If it doesn't then add it.
                if (!bContains)
                    lRemove.Add(pTracker);
            }
            #endregion

            #region Build a list of create candidates.
            // Create an array which contains new trackers to be created.
            List<SpatioTemporalInput> lCreate = new List<SpatioTemporalInput>();
            foreach (SpatioTemporalInput pInput in lInputs)
            {
                // Does the table of best matches already contain this input?
                bool bContains = false;
                for (int i = 0; i < lBest.Count; ++i)
                {
                    if (lBest[i].pInput == pInput)
                    {
                        bContains = true;
                        break;
                    }
                }

                // If it doesn't then add it.
                if (!bContains)
                    lCreate.Add(pInput);
            }
            #endregion

            // So now we need to dispatch some more processing.

            // Update the best matches.
            this.dispatchUpdates(lBest);

            // Remove the old.
            this.removeOld(lRemove);

            // Create the new.
            this.createNew(lCreate);
        }

        /// <summary>
        /// Dispatch updates to trackers which have just consumed a new input.
        /// </summary>
        /// <param name="lPairs">The list of pairs to dispatch.</param>
        private void dispatchUpdates(List<ProcessPair> lPairs)
        {
            // Have the tracker in each pair consume its own input.. How perverse does that sound? :-p
            foreach (ProcessPair pPair in lPairs)
            {
                // Consume it.
                pPair.pTracker.consumeInput(pPair.pInput);

                // Ensure we have a strong lock before firing the event.
                if (pPair.pTracker.StrongLock)
                {
                    // If the tracker state has moved from discovered then good times.
                    if (pPair.pTracker.eTrackerState == TrackerState.Discover)
                    {
                        // Update the state to match a forwarder and raise the 'OnStart' event.
                        pPair.pTracker.eTrackerState = TrackerState.Forward;
                        if (this.OnStart != null)
                            this.OnStart(this, pPair.pTracker);
                    }
                    else
                    {
                        // Raise the event.
                        if (this.OnUpdate != null)
                            this.OnUpdate(this, pPair.pTracker);
                    }
                }
            }
        }

        /// <summary>
        /// Dispatch updates to trackers which have recieved no inputs.
        /// </summary>
        /// <param name="lPairs">The list trackers to remove.</param>
        private void removeOld(List<SpatioTemporalTracker> lTrackers)
        {
            // Remove the old trackers from the list and raise an event for each one saying its happened.
            foreach (SpatioTemporalTracker pRemove in lTrackers)
            {
                // Consume nothing (used so we know how many events before it died).
                pRemove.consumeNothing();

                // If we have strong feeling we lost the lock - kill it.
                if (pRemove.StrongLostLock)
                {
                    // Remove it.
                    this.lTrackers.Remove(pRemove);
                    pRemove.eTrackerState = TrackerState.Destroy;

                    // Raise the event.
                    if (pRemove.StrongLock)
                    {
                        if (this.OnEnd != null)
                            this.OnEnd(this, pRemove);
                    }
                }
            }
        }

        /// <summary>
        /// Dispatch updates to trackers which have just been created.
        /// </summary>
        /// <param name="lPairs">The list trackers to create.</param>
        private void createNew(List<SpatioTemporalInput> lInputs)
        {
            // Create new trackers for each input.
            foreach (SpatioTemporalInput pInput in lInputs)
            {
                // Create new, insert input and then append.
                SpatioTemporalTracker pTracker = new SpatioTemporalTracker(this.DefaultSmoothSize, ++this.iNextID);
                pTracker.consumeInput(pInput);
                pTracker.eTrackerState = TrackerState.Discover;
                lTrackers.Add(pTracker);

                // Raise the event.
                //if (this.OnStart != null)
                //    this.OnStart(this, pTracker);
            }
        }

    }

    /// <summary>
    /// An internally enumerated tracker state that is only known to the Classifier and Tracker.
    /// </summary>
    internal enum TrackerState
    {
        /// <summary>
        /// The tracker has just been discovered and should be raised as a start event once the next move event is recieved.
        /// </summary>
        Discover = 0,
        /// <summary>
        /// The tracker has just been lost and should be raised as a lost event if no more move events are recieved.
        /// </summary>
        Destroy = 1,
        /// <summary>
        /// The tracker has moved from discovered to forward so all subsequent events should be forwarded.
        /// </summary>
        Forward = 2,
    }

    /// <summary>
    /// A SpatioTemporalTracker is a class which represents a context for an existing input.  It is used to determine if a new input bears any relevance to it or is
    /// simply a new point.
    /// </summary>
    public class SpatioTemporalTracker
    {
        #region Static Defaults
        /// <summary>
        /// The number of inputs the tracker should recieve before we are confident it is definately active.
        /// </summary>
        public static uint StrongLockThreshold = 0;

        /// <summary>
        /// The number of non-inputs the tracker should recieve before we are confident it is definately dead. 
        /// </summary>
        public static uint StrongLockLostThreshold = 0;
        #endregion

        #region Properties
        /// <summary>
        /// A reference to the smoothing buffer which is responsible for, well, smoothing the input positions.
        /// </summary>
        public SmoothingBuffer SmoothingBuffer { get; protected set; }

        /// <summary>
        /// Return the smoothed position of the input this tracker is tracking.
        /// </summary>
        public Vector Position { get; protected set; }

        /// <summary>
        /// Return the smoothed normalised position of the input this tracker is tracking.
        /// </summary>
        public Vector NormalPosition { get; protected set; }

        /// <summary>
        /// Return the non-normalised forward vector of this tracker which is essentially the lastpos - currentpos.
        /// </summary>
        public Vector Forward { get; protected set; }

        /// <summary>
        /// Return the normalised forward vector of this tracker which is essentially the lastpos - currentpos scaled by its length.
        /// </summary>
        public Vector NormalForward { get; protected set; }

        /// <summary>
        /// Return the unique tracker number as returned from this classifier.
        /// </summary>
        public ulong ID { get; protected set; }

        /// <summary>
        /// This will return True if we are sure that this tracker is actively recieving input.
        /// </summary>
        public bool StrongLock { get { return iTrackerLock > StrongLockThreshold; } }

        /// <summary>
        /// This will return True if we are sure this tracker has not recieved any recent input.
        /// </summary>
        public bool StrongLostLock { get { return iTrackerLostLock > StrongLockLostThreshold; } }

        /// <summary>
        /// The starting position of this tracker object.
        /// </summary>
        public Vector StartPosition { get; protected set; }
        #endregion

        #region Prediction
        /// <summary>
        /// Return the scale factor for the prediction megnitude.
        /// </summary>
        public double PredictionScale { get; set; }

        /// <summary>
        /// Return the next predicted position of this tracker.
        /// </summary>
        public Vector PredictedNextPosition { get { return new Vector(Position.X + (NormalForward.X * PredictionScale), Position.Y + (NormalForward.Y * PredictionScale)); } }

        /// <summary>
        /// Return the next predicted position of this tracker but in the opposite direction.
        /// </summary>
        public Vector PredictedLastPosition { get { return new Vector(Position.X - (NormalForward.X * PredictionScale), Position.Y - (NormalForward.Y * PredictionScale)); } }
        #endregion

        #region Helpers
        /// <summary>
        /// The classifier-internal state of this tracker (i.e. do we need to treat it spectially because it is existing in an undetermined state?)
        /// </summary>
        internal TrackerState eTrackerState { get; set; }

        /// <summary>
        /// The counter for the number of inputs recieved.
        /// </summary>
        internal int iTrackerLock = 0;

        /// <summary>
        /// The counter for the number of non-inputs recieved.
        /// </summary>
        internal int iTrackerLostLock = 0;
        #endregion

        System.Windows.Forms.Screen primaryScreen;

        /// <summary>
        /// Construct a new tracker with a default smoothing value.
        /// </summary>
        /// <param name="iSmoothSize">The smoothing value.</param>
        /// <param name="iID">The unique session ID for this tracker.</param>
        public SpatioTemporalTracker(int iSmoothSize, ulong iID)//, double fMinRadius, double fMaxRadius, double fRadiusExpansion)
        {
            // Build a smoothing buffer.
            this.SmoothingBuffer = new SmoothingBuffer(iSmoothSize);

            // Save the ID.
            this.ID = iID;

            this.primaryScreen = DeviceUtils.DeviceUtil.GetScreen(Settings.Default.primaryMonitor);

            Settings.Default.PropertyChanged += SettingsChanged;
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;

            // Save the state.
            this.eTrackerState = TrackerState.Discover;
            this.PredictionScale = Math.Max(primaryScreen.Bounds.Width, primaryScreen.Bounds.Height);
        }


        private void SettingsChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "primaryMonitor")
            {
                primaryScreen = DeviceUtils.DeviceUtil.GetScreen(Settings.Default.primaryMonitor);
                this.PredictionScale = Math.Max(primaryScreen.Bounds.Width, primaryScreen.Bounds.Height);
            }
        }
        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            primaryScreen = DeviceUtils.DeviceUtil.GetScreen(Settings.Default.primaryMonitor);
            this.PredictionScale = Math.Max(primaryScreen.Bounds.Width, primaryScreen.Bounds.Height);
        }

        /// <summary>
        /// Take an input which is gaurenteed to be part of this tracker.  Update the tracker based on the input.
        /// </summary>
        /// <param name="pInput">The input we wish to consume.</param>
        public void consumeInput(SpatioTemporalInput pInput)
        {
            // Add the point to the smoothing buffer.
            this.SmoothingBuffer.addValue(pInput.Point);

            // Compute the position as the newly smoothed value.
            Vector tLastPosition = this.Position;
            this.Position = this.SmoothingBuffer.getSmoothedValue();

            // Compute the forward as the difference between the two.
            Forward = new Vector(tLastPosition.X - this.Position.X, tLastPosition.Y - this.Position.Y);

            // Compute the radius as the length of the next predicted.
            //double fLength = Forward.Length * 10;
            //fLength = 100;// Math.Max(50, Math.Min(fLength, 500));
            //if (fLength == 0)
            //    fLength = 100.0f;

            // Compute the normalised forward vector.
            NormalForward = Forward;
            NormalForward.Normalize();

            // Now, if I were handling an adaptive radius, I would update it here to encompass the new predicted position.
            //PredictionScale = fLength;

            // If our tracker lock is 0 then it is sorta-safe to say we are the starting position.
            if (iTrackerLock == 0)
                StartPosition = Position;

            // Increment the tracker lock and reset the lost lock to 0.
            ++iTrackerLock;
            iTrackerLostLock = 0;
        }

        /// <summary>
        /// This is called to notify this tracker that we recieved a situation which made it seem like it was lost.
        /// Internally this increments a counter which affects a 'lost lock', which, when it reaches a threshold becomes sure that we have no more data for it.
        /// </summary>
        public void consumeNothing()
        {
            ++iTrackerLostLock;
        }

        /// <summary>
        /// This function determines a double value which ranks the likelyhood a point is part of this tracker.
        /// Smaller numbers are more likely than larger ones.  Distributions need all the data in order to be normalised with respect to another.
        /// </summary>
        /// <param name="pInput"></param>
        /// <returns>A double word value which ranks the likelyhood of this input being part of this tracker.  Smaller is better.</returns>
        public double getClassificationRanking(SpatioTemporalInput pInput)
        {
            // TODO: TIME IS NOT TAKEN INTO ACCOUNT OF IN THESE EQUATIONS....

            // Compute the distance of the input point to the best and predicted.
            Vector vInput = pInput.Point;
            double fDistanceFromBest = (Position - vInput).Length;
            double fDistanceFromPredicted = (PredictedNextPosition - vInput).Length;

            // Select the smallest of the two lengths (an optimistic algorythm).
            return Math.Min(fDistanceFromBest, fDistanceFromPredicted);
        }

        /// <summary>
        ///  Returns the distance of vPoint to the segment defined by vLineA,vLineB;
        /// </summary>
        /// <param name="vLineA">First point of the segment.</param>
        /// <param name="vLineB">Second point of the segment.</param>
        /// <param name="vPoint">The point to which we want to know the distance of the segment.</param>
        /// <returns>The distance of vPoint to the segment defined by vLineA, vLineB.</returns>
        public static double distanceToSegment(Vector vLineA, Vector vLineB, Vector vPoint)
        {
            // Compute the delta.
            Vector vDelta = vLineB - vLineA;

            // Ensure the arguments are ok.
            if ((vDelta.X == 0) && (vDelta.Y == 0))
            {
                throw new Exception("vLineA and vLineB cannot be the same point");
            }

            // Solve for u: [P3 - P1 - u(P2 - P1)] dot (P2 - P1) = 0 
            double u = ((vPoint.X - vLineA.X) * vDelta.X + (vPoint.Y - vLineA.Y) * vDelta.Y) / (vDelta.X * vDelta.X + vDelta.Y * vDelta.Y);

            // Compute the closest point on the line segment.
            Vector vClosest;
            if (u < 0)
            {
                vClosest = vLineA;
            }
            else if (u > 1)
            {
                vClosest = vLineB;
            }
            else
            {
                vClosest = new Vector(vLineA.X + u * vDelta.X, vLineA.Y + u * vDelta.Y);
            }

            // Return the distance between the closest point on the line and the actual point.
            return (vClosest - vPoint).Length; //vClosest.distance(vPoint);
        }
    }

    /// <summary>
    /// A SpatioTemporalInput describes an input to the SpatioTemporalClassifier.
    /// </summary>
    public class SpatioTemporalInput
    {
        /// <summary>
        /// The point which describes this input.
        /// </summary>
        public Vector Point { get; set; }

        /// <summary>
        /// Construct a new SpatioTemporalInput with coordinate data.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        public SpatioTemporalInput(double x, double y)
        {
            this.Point = new Vector(x, y);
        }
    }
}
