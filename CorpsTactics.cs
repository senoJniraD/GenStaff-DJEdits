using System;
using System.Collections.Generic;
using ModelLib;
using TacticalAILib;

namespace GSBPGEMG
{
    public class CorpsTactics
    {
        public void ImplementCorpsOrder(Game1 game, GameInstance instance, CorpsOrder Corps, Stances Stance, PointI Objective)
        {
            List<PointI> Path;
            Corps.Type = SetCorpsType(Corps);

            if (Corps.Formation == Formations.Line)
            {
                int ClosestUnitID = FindClosestInfantryUnitToObjective(Corps.UnitsInCorps, Objective);
                MATEUnitInstance ClosestUnit = Corps.UnitsInCorps[ClosestUnitID];

                // set order for this unit
                Order TempOrder = new(ClosestUnit);
                TempOrder.Stance = Stances.Attack;
                TempOrder.OrderFormation = Formations.Line;
                if (game.DisplayPlaceInfo)
                    TempOrder.OrderObjective = new(instance.AllPlaces[game.NearestPlaceIndex]);
                else
                    TempOrder.OrderObjective = new(Objective);
                TempOrder.ExecuteOrderTime = instance.CurrentGameTime + TimeSpan.FromMinutes(Corps.OrdersDelay);
                TempOrder.PathToObjective = UtilityMethods.BresenhamLine(TempOrder.UnitSnapshotFormation.Location, Objective);
                instance.CurrentEvents.AddEvent(new Event_DirectOrdersSent([TempOrder], sentByCourier: true));

                string TempReportString = "";
                if (Corps.LineOfType.Length < 1)
                {
                    for (int z = 0; z < Corps.LineOfType.Length; z++)
                        TempReportString += Corps.LineOfType[z];
                }
                else
                {
                    TempReportString = "";
                    for (int z = 0; z < Corps.LineOfType.Length; z++)
                        TempReportString += Corps.LineOfType[z];
                }

                TempReportString += "Order " + ClosestUnit.Name + " to move at " +
                    (instance.CurrentGameTime + TimeSpan.FromMinutes(Corps.OrdersDelay)).ToString("h\\:mm") + " in line formation to ";
                if (TempOrder.OrderObjective.Type == OrderObjective.ObjectiveTypes.Place)
                    TempReportString += TempOrder.OrderObjective.Place.Name + ".";
                if (TempOrder.OrderObjective.Type == OrderObjective.ObjectiveTypes.MapPoint)
                    TempReportString += "coordinates " + TempOrder.OrderObjective.Location.X.ToString() + "/" +
                        TempOrder.OrderObjective.Location.Y.ToString() + " as indicated on the map.";

                //Reports TempReport = new Reports();
                //TempReport.ReportText = TempReportString;
                //TempReport.Type = ReportTypes.CorpsOrders;
                //TempReport.AnimationID = ClosestUnitID;
                //TempReport.AddToReportsList(instance.ActiveArmy);

                ///////////////////////////////////////////////
                // Now set orders for remaining units in corps
                ///////////////////////////////////////////////

                for (int i = 0; i < Corps.UnitsInCorps.Count; i++)
                {
                    if (i == ClosestUnitID)
                        continue;

                    MATEUnitInstance Unit = Corps.UnitsInCorps[i];

                    int Xoffset = Math.Abs(ClosestUnit.Location.X - Unit.Location.X);
                    int Yoffset = Math.Abs(ClosestUnit.Location.Y - Unit.Location.Y);
                    if (ClosestUnit.Location.X > Unit.Location.X)
                        Xoffset *= -1;
                    if (ClosestUnit.Location.Y > Unit.Location.Y)
                        Yoffset *= -1;
                    PointI OffsetObjective = Objective + new PointI(Xoffset, Yoffset);

                    TempOrder = new Order(Unit);
                    if (Unit.UnitType != UnitTypes.HeadQuarters
                        && Unit.UnitType != UnitTypes.Supplies
                        && Unit.UnitType != UnitTypes.Artillery
                        && Unit.UnitType != UnitTypes.HorseArtillery)
                        TempOrder.OrderFormation = Formations.Line;
                    else
                        TempOrder.OrderFormation = Formations.Column;
                    if (game.DisplayPlaceInfo)
                        TempOrder.OrderObjective = new(instance.AllPlaces[game.NearestPlaceIndex]);
                    else
                        TempOrder.OrderObjective = new(OffsetObjective);
                    TempOrder.ExecuteOrderTime = instance.CurrentGameTime + TimeSpan.FromMinutes(Corps.OrdersDelay);
                    TempOrder.PathToObjective = UtilityMethods.BresenhamLine(TempOrder.UnitSnapshotFormation.Location, OffsetObjective);
                    instance.CurrentEvents.AddEvent(new Event_DirectOrdersSent([TempOrder], sentByCourier: true));

                    if (Corps.LineOfType.Length < 1)
                    {
                        for (int z = 0; z < Corps.LineOfType.Length; z++)
                            TempReportString += Corps.LineOfType[z];
                    }
                    else
                    {
                        TempReportString = "";
                        for (int z = 0; z < Corps.LineOfType.Length; z++)
                            TempReportString += Corps.LineOfType[z];
                    }

                    TempReportString += "Order " + Unit.Name + " to move at " +
                        (instance.CurrentGameTime + TimeSpan.FromMinutes(Corps.OrdersDelay)).ToString("h\\:mm") + " in " +
                        TempOrder.OrderFormation + "formation to ";
                    if (game.DisplayPlaceInfo)
                        TempReportString += instance.AllPlaces[game.NearestPlaceIndex].Name + ".";
                    else
                        TempReportString += "coordinates " + TempOrder.OrderObjective.Location.X.ToString() + "/" +
                            TempOrder.OrderObjective.Location.Y.ToString() + " as indicated on the map.";

                    //TempReport = new Reports();
                    //TempReport.ReportText = TempReportString;
                    //TempReport.Type = ReportTypes.CorpsOrders;
                    //TempReport.AnimationID = Unit.Index;
                    //TempReport.AddToReportsList(ActiveArmy);
                }
            }
            else // best path formation
            {
                for (int i = 0; i < Corps.UnitsInCorps.Count; i++)
                {
                    // Getting the path
                    // should check for null, no path
                    MATEUnitInstance corpsUnit = Corps.UnitsInCorps[i];
                    Path = Corps.Commander.Army.MapPathfinding.GetPathFromA2B(corpsUnit.Orders.LatestObjectiveSnapshot.Location, Objective, corpsUnit,
                        MapPathfinding.WallTypes.ROIOpponentArmy);

                    Order TempOrder = new Order(corpsUnit);
                    TempOrder.Stance = Stance;
                    TempOrder.OrderFormation = Formations.Column;
                    if (game.DisplayPlaceInfo)
                        TempOrder.OrderObjective = new(instance.AllPlaces[game.NearestPlaceIndex]);
                    else
                        TempOrder.OrderObjective = new(Objective);
                    TempOrder.ExecuteOrderTime = instance.CurrentGameTime + TimeSpan.FromMinutes(Corps.OrdersDelay);
                    TempOrder.PathToObjective = Path;
                    //if (TempOrder.Path2Objective.Count >= 2) // TODO (noted by MT) - needed?
                    //    TempOrder.OrderFacing = TempOrder.Path2Objective[^(Math.Min(TempOrder.Path2Objective.Count, 15))].AngleToFacePointInRadians(
                    //        TempOrder.Path2Objective[^1]);
                    //else
                    //    TempOrder.OrderFacing = TempOrder.UnitSnapshotStart.Location.AngleToFacePointInRadians(TempOrder.Path2Objective[^1]);

                    // Let's check to see if we're clicking on an enemy unit!
                    MATEUnitInstance OpponentUnit = game.MouseOverUnitSnapshot(instance.OpponentArmy.Units);
                    if (OpponentUnit != null && corpsUnit.CanSkirmish != Skirmishing.Always)
                    {
                        TempOrder.Stance = Stances.Attack;
                        TempOrder.OrderObjective = new(OpponentUnit);
                        TempOrder.OrderFormation = Formations.Column;
                    }

                    instance.CurrentEvents.AddEvent(new Event_DirectOrdersSent([TempOrder], sentByCourier: true));

                    //if (Corps.LineOfType.Length < 1)
                    //{
                    //    for (int z = 0; z < Corps.LineOfType.Length; z++)
                    //        TempReportString += Corps.LineOfType[z];
                    //}
                    //else
                    //{
                    //    TempReportString = "";
                    //    for (int z = 0; z < Corps.LineOfType.Length; z++)
                    //        TempReportString += Corps.LineOfType[z];
                    //}

                    //TempReportString += "Order " + corpsUnit.Name + " to move at " +
                    //    (CurrentGameTime + TimeSpan.FromMinutes(Corps.OrdersDelay)).ToString("h\\:mm") +
                    //    " in column formation utilizing roads to ";
                    //if (TempOrder.OrderObjective.Type == OrderObjective.ObjectiveTypes.Place)
                    //    TempReportString += TempOrder.OrderObjective.Place.Name + ".";
                    //else
                    //    TempReportString += "coordinates " + TempOrder.OrderObjective.Location.X.ToString() + "/" +
                    //        TempOrder.OrderObjective.Location.Y.ToString() + " as indicated on the map.";

                    //Reports TempReport = new Reports();
                    //TempReport.ReportText = TempReportString;
                    //TempReport.Type = ReportTypes.CorpsOrders;
                    //TempReport.AnimationID = corpsUnit.Index;
                    //TempReport.AddToReportsList(ActiveArmy); // need to strip out all report stuff
                }
            }
        }

        // NB: This is the 'human' implementation of FindClosestInfantryUnit2Objective()
        public static int FindClosestInfantryUnitToObjective(List<MATEUnitInstance> UnitsInCorps, PointI Objective)
        {
            int ClosestUnitID = -1;
            double ClosestDistance = double.MaxValue;

            for (int i = 0; i < UnitsInCorps.Count; i++)
            {
                if (UnitsInCorps[i].IsInfantryType)
                {
                    double Distance = Map.EuclideanDistance(UnitsInCorps[i].Location, Objective);
                    if (Distance < ClosestDistance)
                    {
                        ClosestDistance = Distance;
                        ClosestUnitID = i;
                    }
                }
            }

            return ClosestUnitID;
        }

        //public static void CancelCorpsOrders() // TODO (noted by MT)
        //{
        //    for (int i = 0; i < ActiveArmy.CorpsList[TabSubordinates.DisplayCorpsIndex].UnitsInCorps.Count; i++)
        //    {
        //      ActiveArmy.CorpsList[TabSubordinates.DisplayCorpsIndex].UnitsInCorps[i].Commands.RemoveAt(
        //     ActiveArmy.CorpsList[TabSubordinates.DisplayCorpsIndex].UnitsInCorps[i].Commands.Count - 1);
        //    }
        //}

        public static CorpsOrder.CorpsTypes SetCorpsType(CorpsOrder Corps)
        {
            CorpsOrder.CorpsTypes CType;
            int NumCav = 0;
            int NumInf = 0;
            int NumArt = 0;

            for (int i = 0; i < Corps.UnitsInCorps.Count; i++)
            {
                if (Corps.UnitsInCorps[i].IsInfantryType)
                    NumInf++;

                else if (Corps.UnitsInCorps[i].IsInfantryType ||
                    Corps.UnitsInCorps[i].UnitType == UnitTypes.HorseArtillery)
                    NumCav++;

                else if (Corps.UnitsInCorps[i].UnitType == UnitTypes.Artillery)
                    NumArt++;
            }

            if (NumInf != 0 && NumCav == 0 & NumArt == 0)
                CType = CorpsOrder.CorpsTypes.Infantry;
            else if (NumInf == 0 && NumCav != 0 & NumArt == 0)
                CType = CorpsOrder.CorpsTypes.Cavalry;
            else if (NumInf == 0 && NumCav == 0 & NumArt != 0)
                CType = CorpsOrder.CorpsTypes.Artillery;
            else
                CType = CorpsOrder.CorpsTypes.Mixed;

            return CType;
        }
    }
}
