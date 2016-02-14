﻿using System;
using System.Collections.Generic;
using System.Linq;
using USITools.Logistics;

namespace PlanetaryLogistics
{
    public class ModulePlanetaryLogistics : PartModule
    {
        [KSPField]
        public double CheckFrequency = 12d;
        
        [KSPField]
        public double LowerTrigger = .25d;

        [KSPField]
        public double UpperTrigger = .75d;

        [KSPField]
        public double FillGoal = .5d;

        [KSPField] 
        public double vesselIdTax = 0.05d;

        private double lastCheck;

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            if (!vessel.LandedOrSplashed)
                return;

            if (Math.Abs(Planetarium.GetUniversalTime() - lastCheck) < CheckFrequency)
                return;

            lastCheck = Planetarium.GetUniversalTime();
            foreach (var res in part.Resources.list)
            {
                LevelvesselIds(res.resourceName);
            }
        }


        private void LevelvesselIds(string vesselId)
        {
            var res = part.Resources[vesselId];
            var body = vessel.mainBody.flightGlobalsIndex;
            var fillPercent = res.amount / res.maxAmount;
            if (fillPercent < LowerTrigger)
            {
                var amtNeeded = (res.maxAmount * FillGoal) - res.amount;
                if (!(amtNeeded > 0)) 
                    return;
                
                if (!PlanetaryLogisticsManager.Instance.DoesLogEntryExist(vesselId, body)) 
                    return;
                
                var logEntry = PlanetaryLogisticsManager.Instance.FetchLogEntry(vesselId, body);
                if (logEntry.StoredQuantity > amtNeeded)
                {
                    logEntry.StoredQuantity -= amtNeeded;
                    res.amount += amtNeeded;
                }
                else
                {
                    res.amount += logEntry.StoredQuantity;
                    logEntry.StoredQuantity = 0;
                }
                PlanetaryLogisticsManager.Instance.TrackLogEntry(logEntry);
            }

            else if (fillPercent > UpperTrigger)
            {
                var strAmt = res.amount - (res.maxAmount * FillGoal);
                if (!(strAmt > 0)) 
                    return;
                
                var logEntry = PlanetaryLogisticsManager.Instance.FetchLogEntry(vesselId, body);
                logEntry.StoredQuantity += (strAmt * (1d-vesselIdTax));
                res.amount -= strAmt;
                PlanetaryLogisticsManager.Instance.TrackLogEntry(logEntry);
            }
        }
    }
}