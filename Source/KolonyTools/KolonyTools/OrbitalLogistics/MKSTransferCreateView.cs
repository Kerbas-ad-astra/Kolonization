using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KolonyTools
{
    public class MKSTransferCreateView : Window<MKSTransferCreateView>
    {
        private struct TransferValidationResult
        {
            internal bool EnoughTransferResources;
            internal bool EnoughTransferCostResources;
            internal string ValidationMessage;

            internal TransferValidationResult(bool tRes, bool tcRes, string msg)
            {
                this.EnoughTransferCostResources = tcRes;
                this.EnoughTransferResources = tRes;
                this.ValidationMessage = msg;
            }

            internal bool EnoughRes
            {
                get { return this.EnoughTransferCostResources && this.EnoughTransferResources; }
            }
        }

        private readonly MKSLGuiTransfer _model;
        private readonly MKSLcentral _central;
        private Vector2 scrollPositionEditGUIResources;
        private MKSLresource editGUIResource;
        private string StrAmount;
        private double currentAvailable;
        private double[] currentTargetAmounts;
        private string StrValidationMessage;
        private int vesselFrom;
        private int vesselTo;
        private Dictionary<TransferCostPaymentModes, bool> _costPayModes;

        private ComboBox fromVesselComboBox;

        private ComboBox toVesselComboBox;

        private void Start()
        {
            var listStyle = new GUIStyle();
            var fromList = _central.bodyVesselList.Select(x => new GUIContent(x.vesselName)).ToArray();
            var toList = _central.bodyVesselList.Select(x => new GUIContent(x.vesselName)).ToArray();
            //comboBoxList = new GUIContent[5];
            //comboBoxList[0] = new GUIContent("Thing 1");
            //comboBoxList[1] = new GUIContent("Thing 2");
            //comboBoxList[2] = new GUIContent("Thing 3");
            //comboBoxList[3] = new GUIContent("Thing 4");
            //comboBoxList[4] = new GUIContent("Thing 5");
            listStyle.normal.textColor = Color.white;
            listStyle.onHover.background =
                listStyle.hover.background = new Texture2D(2, 2);
            listStyle.padding.left =
                listStyle.padding.right =
                    listStyle.padding.top =
                        listStyle.padding.bottom = 4;
            this._costPayModes = new Dictionary<TransferCostPaymentModes, bool>
            {
                {TransferCostPaymentModes.Source, false},
                {TransferCostPaymentModes.Target, false},
                {TransferCostPaymentModes.Both, true}
            };

            fromVesselComboBox = new ComboBox(new Rect(20, 30, 100, 20), fromList[0], fromList, "button", "box", listStyle,
                i =>
                {
                    vesselFrom = i;
                    _model.VesselFrom = _central.bodyVesselList[i];
                    _model.calcResources();
                });
            fromVesselComboBox.SelectedItemIndex = _central.bodyVesselList.IndexOf(_model.VesselFrom);
            toVesselComboBox = new ComboBox(new Rect(20, 30, 100, 20), toList[0], toList, "button", "box", listStyle,
                i =>
                {
                    vesselTo = i;
                    _model.VesselTo = _central.bodyVesselList[i];
                    _model.calcResources();
                });
            toVesselComboBox.SelectedItemIndex = _central.bodyVesselList.IndexOf(_model.VesselTo);
        }

        public MKSTransferCreateView(MKSLGuiTransfer model, MKSLcentral central)
            : base(model.transferName, 400, 450)
        {
            _model = model;
            _central = central;
            Start();
            SetVisible(true);
        }

        protected override void DrawWindowContents(int windowId)
        {
            Func<TransferCostPaymentModes> getSelectedMode = () =>
            {
                return (this._costPayModes.First(kv => kv.Value)).Key;
            };
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<<", MKSGui.buttonStyle, GUILayout.Width(40)))
            {
                previousBodyVesselList(ref vesselFrom);
                _model.VesselFrom = _central.bodyVesselList[vesselFrom];
                fromVesselComboBox.SelectedItemIndex = vesselFrom;
                _model.calcResources();
            }
            GUILayout.Label("From:", MKSGui.labelStyle, GUILayout.Width(60));
            fromVesselComboBox.Show();
            //GUILayout.Label(_model.VesselFrom.vesselName, MKSGui.labelStyle, GUILayout.Width(160));
            if (GUIButton.LayoutButton(new GUIContent(">>"), MKSGui.buttonStyle, GUILayout.Width(40)))
            {
                nextBodyVesselList(ref vesselFrom);
                _model.VesselFrom = _central.bodyVesselList[vesselFrom];
                fromVesselComboBox.SelectedItemIndex = vesselFrom;
                _model.calcResources();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<<", MKSGui.buttonStyle, GUILayout.Width(40)))
            {
                previousBodyVesselList(ref vesselTo);
                _model.VesselTo = _central.bodyVesselList[vesselTo];
                toVesselComboBox.SelectedItemIndex = vesselTo;
                _model.calcResources();
            }
            GUILayout.Label("To:", MKSGui.labelStyle, GUILayout.Width(60));
            toVesselComboBox.Show();
            //GUILayout.Label(_model.VesselTo.vesselName, MKSGui.labelStyle, GUILayout.Width(160));
            if (GUIButton.LayoutButton(new GUIContent(">>"), MKSGui.buttonStyle, GUILayout.Width(40)))
            {
                nextBodyVesselList(ref vesselTo);
                _model.VesselTo = _central.bodyVesselList[vesselTo];
                toVesselComboBox.SelectedItemIndex = vesselTo;
                _model.calcResources();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            scrollPositionEditGUIResources = GUILayout.BeginScrollView(scrollPositionEditGUIResources, GUILayout.Width(300), GUILayout.Height(150));
            foreach (MKSLresource res in _model.transferList)
            {
                GUILayout.BeginHorizontal();
                if (GUIButton.LayoutButton(new GUIContent(res.resourceName + ": " + Math.Round(res.amount, 2) + " of " +
                                                          Math.Round(_model.resourceAmount.Find(x => x.resourceName == res.resourceName).amount))))
                {
                    editGUIResource = res;
                    StrAmount = Math.Round(res.amount, 2).ToString();
                    currentAvailable = readResource(_model.VesselFrom, editGUIResource.resourceName)[0];
                    currentTargetAmounts = this.readResource(_model.VesselTo, editGUIResource.resourceName);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.BeginVertical();

            if (editGUIResource != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Resource:", MKSGui.labelStyle, GUILayout.Width(80));
                GUILayout.Label(editGUIResource.resourceName, MKSGui.labelStyle, GUILayout.Width(100));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Amount:", MKSGui.labelStyle, GUILayout.Width(80));
                StrAmount = GUILayout.TextField(StrAmount, 10, MKSGui.textFieldStyle, GUILayout.Width(60));
                Action<double> setAmount = (a) =>
                {
                    if (a < currentAvailable)
                    {
                        editGUIResource.amount = a;
                    }
                    else
                    {
                        editGUIResource.amount = currentAvailable;
                        StrAmount = editGUIResource.amount.ToString("F2");
                    }
                };
                if (GUILayout.Button("Set", MKSGui.buttonStyle, GUILayout.Width(30)))
                {
                    double number;
                    if (Double.TryParse(StrAmount, out number))
                    {
                        setAmount(number);
                    }
                    else
                    {
                        StrAmount = "0";
                        editGUIResource.amount = 0;
                    }
                    updateCostList(_model);
                    validateTransfer(_model, getSelectedMode(), ref StrValidationMessage);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Mass:", MKSGui.labelStyle, GUILayout.Width(80));
                GUILayout.Label(Math.Round(editGUIResource.mass(), 2).ToString(), MKSGui.labelStyle, GUILayout.Width(100));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Available:", MKSGui.labelStyle, GUILayout.Width(80));
                GUILayout.Label(Math.Round(currentAvailable, 2).ToString(), MKSGui.labelStyle, GUILayout.Width(100));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Target:", MKSGui.labelStyle, GUILayout.Width(80));
                GUILayout.Label(string.Format("{0:F2}/{1:F2}", currentTargetAmounts[0], currentTargetAmounts[1]), MKSGui.labelStyle, GUILayout.Width(100));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Fill Up", MKSGui.buttonStyle, GUILayout.Width(100)))
                {
                    var diff = Math.Round(currentTargetAmounts[1] - currentTargetAmounts[0], 2);
                    if (diff >= 0)
                    {
                        setAmount(diff);
                        StrAmount = diff.ToString("F2");
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Label("Tranfer Mass: " + Math.Round(_model.totalMass(), 2) + " (maximum: " + _central.maxTransferMass + ")", MKSGui.labelStyle, GUILayout.Width(300));


            GUILayout.BeginHorizontal();
            GUILayout.Label("");
            if (_central.Mix1CostName != "")
            {
                if (GUILayout.Button(_central.Mix1CostName, MKSGui.buttonStyle, GUILayout.Width(170)))
                {
                    _model.initCostList(_central.Mix1CostResources);
                    updateCostList(_model);
                }
            }
            if (_central.Mix2CostName != "")
            {
                if (GUILayout.Button(_central.Mix2CostName, MKSGui.buttonStyle, GUILayout.Width(170)))
                {
                    _model.initCostList(_central.Mix2CostResources);
                    updateCostList(_model);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (_central.Mix3CostName != "")
            {
                if (GUILayout.Button(_central.Mix3CostName, MKSGui.buttonStyle, GUILayout.Width(170)))
                {
                    _model.initCostList(_central.Mix3CostResources);
                    updateCostList(_model);
                }
            }
            if (_central.Mix4CostName != "")
            {
                if (GUILayout.Button(_central.Mix4CostName, MKSGui.buttonStyle, GUILayout.Width(170)))
                {
                    _model.initCostList(_central.Mix4CostResources);
                    updateCostList(_model);
                }
            }
            GUILayout.EndHorizontal();

            foreach (MKSLresource resCost in _model.costList)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(resCost.resourceName + ":", MKSGui.labelStyle, GUILayout.Width(100));
                GUILayout.Label(Math.Round(resCost.amount, 2).ToString(), MKSGui.labelStyle, GUILayout.Width(200));
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Transfer cost paid by: ", MKSGui.labelStyle, GUILayout.Width(170));
            if (this._checkTransferAmounts(this._model, TransferCostPaymentModes.Both).EnoughRes)
            {
                var oldVal = this._costPayModes[TransferCostPaymentModes.Both];
                this._costPayModes[TransferCostPaymentModes.Both] = GUILayout.Toggle(this._costPayModes[TransferCostPaymentModes.Both], "Both", MKSGui.buttonStyle);
                if (oldVal != this._costPayModes[TransferCostPaymentModes.Both])
                {
                    this._costPayModes[TransferCostPaymentModes.Source] = this._costPayModes[TransferCostPaymentModes.Target] = false;
                }
            }
            if (this._checkTransferAmounts(this._model, TransferCostPaymentModes.Source).EnoughRes)
            {
                var oldVal = this._costPayModes[TransferCostPaymentModes.Source];
                this._costPayModes[TransferCostPaymentModes.Source] = GUILayout.Toggle(this._costPayModes[TransferCostPaymentModes.Source], "Source", MKSGui.buttonStyle);
                if (oldVal != this._costPayModes[TransferCostPaymentModes.Source])
                {
                    this._costPayModes[TransferCostPaymentModes.Both] = this._costPayModes[TransferCostPaymentModes.Target] = false;
                }
            }
            if (this._checkTransferAmounts(this._model, TransferCostPaymentModes.Target).EnoughRes)
            {
                var oldVal = this._costPayModes[TransferCostPaymentModes.Target];
                this._costPayModes[TransferCostPaymentModes.Target] = GUILayout.Toggle(this._costPayModes[TransferCostPaymentModes.Target], "Target", MKSGui.buttonStyle);
                if (oldVal != this._costPayModes[TransferCostPaymentModes.Target])
                {
                    this._costPayModes[TransferCostPaymentModes.Both] = this._costPayModes[TransferCostPaymentModes.Source] = false;
                }
            }
            if (!(this._costPayModes[TransferCostPaymentModes.Both] || this._costPayModes[TransferCostPaymentModes.Source] || this._costPayModes[TransferCostPaymentModes.Target]))
            {
                this._costPayModes[TransferCostPaymentModes.Both] = true;
            }
            GUILayout.EndHorizontal();

            updateArrivalTime(_model);
            GUILayout.Label("Transfer time: " + Utilities.DeliveryTimeString(_model.arrivaltime, Planetarium.GetUniversalTime()));

            GUILayout.Label(StrValidationMessage, MKSGui.redlabelStyle);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Initiate Transfer", MKSGui.buttonStyle, GUILayout.Width(200)))
            {
                updateCostList(_model);
                var selMode = getSelectedMode();
                if (validateTransfer(_model, selMode, ref StrValidationMessage))
                {
                    createTransfer(_model, selMode);
                }
            }
            if (GUILayout.Button("Cancel", MKSGui.buttonStyle, GUILayout.Width(100)))
            {
                SetVisible(false);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            fromVesselComboBox.ShowRest();
            toVesselComboBox.ShowRest();
        }

        private TransferValidationResult _checkTransferAmounts(MKSLtransfer trans, TransferCostPaymentModes paymentMode)
        {
            var res = new[] { true, true };
            var validationMess = "";
            var totals = new Dictionary<string, double>();
            foreach (var tRes in trans.transferList)
            {
                totals.Add(tRes.resourceName, tRes.amount);
                if (this.readResource(trans.VesselFrom, tRes.resourceName)[0] < tRes.amount)
                {
                    res[0] = res[1] = false;
                    validationMess = validationMess + "insufficient " + tRes.resourceName + "    ";
                    break;
                }
            }
            if (res[1])
            {
                Func<string, double, double> subtractTransfer = (n, a) =>
                {
                    if (totals.ContainsKey(n))
                    {
                        return a - totals[n];
                    }
                    return a;
                };
                foreach (var cRes in trans.costList)
                {
                    var totalAvail = 0d;
                    switch (paymentMode)
                    {
                        case TransferCostPaymentModes.Source:
                            {
                                totalAvail = subtractTransfer(cRes.resourceName, this.readResource(trans.VesselFrom, cRes.resourceName)[0]);
                            }
                            break;
                        case TransferCostPaymentModes.Target:
                            {
                                totalAvail = this.readResource(trans.VesselTo, cRes.resourceName)[0];
                            }
                            break;
                        case TransferCostPaymentModes.Both:
                            {
                                totalAvail = this.readResource(trans.VesselTo, cRes.resourceName)[0] + subtractTransfer(cRes.resourceName, this.readResource(trans.VesselFrom, cRes.resourceName)[0]);

                            } break;
                    }
                    if (!(totalAvail < cRes.amount))
                    {
                        continue;
                    }
                    validationMess = validationMess + "insufficient " + cRes.resourceName + "    ";
                    res[1] = false;
                    break;
                }
            }
            return new TransferValidationResult(res[0], res[1], validationMess);
        }

        public void updateCostList(MKSLtransfer trans)
        {
            double PSSM = getValueFromStrPlanet(_central.DistanceModifierPlanet, _central.vessel.mainBody.name);
            double PSOM = getValueFromStrPlanet(_central.SurfaceOrbitModifierPlanet, _central.vessel.mainBody.name);
            double POSM = getValueFromStrPlanet(_central.OrbitSurfaceModifierPlanet, _central.vessel.mainBody.name);

            double ATUP = 1;
            double ATDO = 1;
            if (_central.vessel.mainBody.atmosphere)
            {
                ATUP = (double)_central.AtmosphereUpModifier;
                ATDO = (double)_central.AtmosphereDownModifier;
            }

            foreach (MKSLresource res in trans.costList)
            {
                ///take into account amount

                ///take into account celestialbody
                if ((trans.VesselFrom.protoVessel.situation == Vessel.Situations.LANDED || trans.VesselFrom.protoVessel.situation == Vessel.Situations.SPLASHED || trans.VesselFrom.protoVessel.situation == Vessel.Situations.PRELAUNCH) &&
                    (trans.VesselTo.protoVessel.situation == Vessel.Situations.LANDED || trans.VesselTo.protoVessel.situation == Vessel.Situations.SPLASHED))
                {
                    double distance = _central.GetDistanceBetweenPoints(trans.VesselFrom.protoVessel.latitude, trans.VesselFrom.protoVessel.longitude, trans.VesselTo.protoVessel.latitude, trans.VesselTo.protoVessel.longitude);
                    res.amount = res.costPerMass * trans.totalMass() * distance * _central.vessel.mainBody.GeeASL * _central.DistanceModifier * PSSM;
                }
                else if ((trans.VesselFrom.protoVessel.situation == Vessel.Situations.LANDED || trans.VesselFrom.protoVessel.situation == Vessel.Situations.SPLASHED || trans.VesselFrom.protoVessel.situation == Vessel.Situations.PRELAUNCH) &&
                         (trans.VesselTo.protoVessel.situation == Vessel.Situations.ORBITING))
                {
                    res.amount = res.costPerMass * trans.totalMass() * _central.vessel.mainBody.GeeASL * _central.vessel.mainBody.Radius * ATUP * _central.SurfaceOrbitModifier * PSOM;
                }
                else if ((trans.VesselFrom.protoVessel.situation == Vessel.Situations.ORBITING) &&
                         (trans.VesselTo.protoVessel.situation == Vessel.Situations.LANDED || trans.VesselTo.protoVessel.situation == Vessel.Situations.SPLASHED || trans.VesselFrom.protoVessel.situation == Vessel.Situations.PRELAUNCH))
                {
                    res.amount = res.costPerMass * trans.totalMass() * _central.vessel.mainBody.GeeASL * _central.vessel.mainBody.Radius * ATDO * _central.OrbitSurfaceModifier * POSM;
                }
                else //Working code - going to just use the same calc as surface to surface for orbit to orbit for now
                {
                    double distance = _central.GetDistanceBetweenPoints(trans.VesselFrom.protoVessel.latitude, trans.VesselFrom.protoVessel.longitude, trans.VesselTo.protoVessel.latitude, trans.VesselTo.protoVessel.longitude);
                    res.amount = res.costPerMass * trans.totalMass() * distance * _central.vessel.mainBody.GeeASL * _central.DistanceModifier * PSSM;
                }

            }
        }
        internal bool validateTransfer(MKSLtransfer trans, TransferCostPaymentModes mode, ref string validationMess)
        {
            bool check = true;
            validationMess = "";


            //check if origin is not the same as destination
            if (trans.VesselFrom.id.ToString() == trans.VesselTo.id.ToString())
            {
                validationMess = "origin and destination are equal";
                return false;
            }

            //check situation origin vessel
            if (trans.VesselFrom.situation != Vessel.Situations.ORBITING && trans.VesselFrom.situation != Vessel.Situations.SPLASHED && trans.VesselFrom.situation != Vessel.Situations.LANDED && trans.VesselFrom.situation != Vessel.Situations.PRELAUNCH)
            {
                validationMess = "origin of transfer is not in a stable situation";
                return false;
            }

            //check situation destination vessel
            if (trans.VesselTo.situation != Vessel.Situations.ORBITING && trans.VesselTo.situation != Vessel.Situations.SPLASHED && trans.VesselTo.situation != Vessel.Situations.LANDED && trans.VesselFrom.situation != Vessel.Situations.LANDED)
            {
                validationMess = "destination of transfer is not in a stable situation";
                return false;
            }



            ////check for sufficient transfer resources
            //foreach (MKSLresource transRes in trans.transferList)
            //{
            //    if (readResource(trans.VesselFrom, transRes.resourceName)[0] < transRes.amount)
            //    {
            //        check = false;
            //        validationMess = validationMess + "insufficient " + transRes.resourceName + "    ";
            //    }
            //}

            ////check for sufficient cost resources

            //foreach (MKSLresource costRes in trans.costList)
            //{
            //    double totalResAmount = 0;

            //    totalResAmount = costRes.amount;

            //    foreach (MKSLresource transRes in trans.transferList)
            //    {
            //        if (costRes.resourceName == transRes.resourceName)
            //        {
            //            totalResAmount = totalResAmount + transRes.amount;
            //        }
            //    }

            //    if ((readResource(trans.VesselFrom, costRes.resourceName)[0] + readResource(_central.vessel, costRes.resourceName)[0]) < totalResAmount)
            //    {
            //        check = false;
            //        validationMess = validationMess + "insufficient " + costRes.resourceName + "    ";
            //    }
            //}

            var checkRes = this._checkTransferAmounts(trans, mode);
            check = checkRes.EnoughRes;

            if (check)
            {
                validationMess = "";
                return true;
            }
            validationMess = checkRes.ValidationMessage;
            return false;
        }

        internal void createTransfer(MKSLtransfer trans, TransferCostPaymentModes mode)
        {
            trans.costList = trans.costList.Where(x => x.amount > 0).ToList();
            trans.transferList = trans.transferList.Where(x => x.amount > 0).ToList();

            foreach (MKSLresource costRes in trans.costList)
            {
                var toDraw = -costRes.amount;
                switch (mode)
                {
                    case TransferCostPaymentModes.Source:
                        {
                            trans.VesselFrom.ExchangeResources(costRes.resourceName, toDraw);
                        }
                        break;
                    case TransferCostPaymentModes.Target:
                        {
                            trans.VesselTo.ExchangeResources(costRes.resourceName, toDraw);
                        }
                        break;
                    case TransferCostPaymentModes.Both:
                    default:
                        {
                            Vessel first;
                            Vessel second;
                            if (this._central.vessel.id == trans.VesselFrom.id)
                            {
                                first = trans.VesselFrom;
                                second = trans.VesselTo;
                            }
                            else
                            {
                                first = trans.VesselTo;
                                second = trans.VesselFrom;
                            }
                            var drawn = first.ExchangeResources(costRes.resourceName, toDraw);
                            second.ExchangeResources(costRes.resourceName, toDraw - drawn);
                        }
                        break;
                }
            }

            foreach (MKSLresource transRes in trans.transferList)
            {
                transRes.amount = -trans.VesselFrom.ExchangeResources(transRes.resourceName, -transRes.amount);
            }

            trans.delivered = false;
            updateArrivalTime(trans);

            if (trans.VesselTo.situation == Vessel.Situations.ORBITING)
            {
                trans.orbit = true;
                trans.SMA = trans.VesselTo.protoVessel.orbitSnapShot.semiMajorAxis;
                trans.ECC = trans.VesselTo.protoVessel.orbitSnapShot.eccentricity;
                trans.INC = trans.VesselTo.protoVessel.orbitSnapShot.inclination;
            }

            if (trans.VesselTo.situation == Vessel.Situations.LANDED || trans.VesselTo.situation == Vessel.Situations.SPLASHED || trans.VesselFrom.situation == Vessel.Situations.LANDED)
            {
                trans.surface = true;
                trans.LON = trans.VesselTo.protoVessel.longitude;
                trans.LAT = trans.VesselTo.protoVessel.latitude;
            }

            _central.saveCurrentTransfersList.Add(trans);
            SetVisible(false);
        }
        public double[] readResource(Vessel ves, string ResourceName)
        {
            double amountCounted = 0d;
            double maxAmountCounted = 0d;
            if (ves.packed && !ves.loaded)
            {
                //Thanks to NathanKell for explaining how to access and edit parts of unloaded vessels and pointing me for some example code is NathanKell's own Mission Controller Extended mod!
                foreach (ProtoPartSnapshot p in ves.protoVessel.protoPartSnapshots)
                {
                    foreach (ProtoPartResourceSnapshot r in p.resources)
                    {
                        if (r.resourceName == ResourceName)
                        {
                            amountCounted = amountCounted + Convert.ToDouble(r.resourceValues.GetValue("amount"));
                            maxAmountCounted += Convert.ToDouble(r.resourceValues.GetValue("maxAmount"));
                        }
                    }
                }
            }
            else
            {
                foreach (Part p in ves.parts)
                {
                    foreach (PartResource r in p.Resources)
                    {
                        if (r.resourceName == ResourceName)
                        {
                            amountCounted = amountCounted + r.amount;
                            maxAmountCounted += r.maxAmount;
                        }
                    }
                }
            }
            return new[] { amountCounted, maxAmountCounted };
        }
        private double getValueFromStrPlanet(string StrPlanet, string PlanetName)
        {
            string[] planets = StrPlanet.Split(',');
            foreach (String planet in planets)
            {
                string[] planetInfo = planet.Split(':');
                if (planetInfo[0] == PlanetName)
                    return (Convert.ToDouble(planetInfo[1]));
            }
            return (1);
        }
        public void updateArrivalTime(MKSLtransfer trans)
        {
            double prepT = (double)_central.PrepTime;
            double TpD = getValueFromStrPlanet(_central.TimePerDistancePlanet, _central.vessel.mainBody.name);
            if (1 == TpD)
            {
                TpD = _central.TimePerDistance;
            }
            double TtfLO = getValueFromStrPlanet(_central.TimeToFromLOPlanet, _central.vessel.mainBody.name);
            if (1 == TtfLO)
            {
                TtfLO = _central.TimeToFromLO;
            }

            if ((trans.VesselFrom.protoVessel.situation == Vessel.Situations.LANDED || trans.VesselFrom.protoVessel.situation == Vessel.Situations.SPLASHED || trans.VesselFrom.protoVessel.situation == Vessel.Situations.PRELAUNCH) &&
                (trans.VesselTo.protoVessel.situation == Vessel.Situations.LANDED || trans.VesselTo.protoVessel.situation == Vessel.Situations.SPLASHED || trans.VesselTo.protoVessel.situation == Vessel.Situations.PRELAUNCH))
            {
                double distance = _central.GetDistanceBetweenPoints(trans.VesselFrom.protoVessel.latitude, trans.VesselFrom.protoVessel.longitude, trans.VesselTo.protoVessel.latitude, trans.VesselTo.protoVessel.longitude);
                trans.arrivaltime = Planetarium.GetUniversalTime() + prepT + (distance * TpD);
            }
            else if ((trans.VesselFrom.protoVessel.situation == Vessel.Situations.LANDED || trans.VesselFrom.protoVessel.situation == Vessel.Situations.SPLASHED || trans.VesselFrom.protoVessel.situation == Vessel.Situations.PRELAUNCH) &&
                     (trans.VesselTo.protoVessel.situation == Vessel.Situations.ORBITING))
            {
                trans.arrivaltime = Planetarium.GetUniversalTime() + prepT + TtfLO;
            }
            else if ((trans.VesselFrom.protoVessel.situation == Vessel.Situations.ORBITING) &&
                     (trans.VesselTo.protoVessel.situation == Vessel.Situations.LANDED || trans.VesselTo.protoVessel.situation == Vessel.Situations.SPLASHED || trans.VesselTo.protoVessel.situation == Vessel.Situations.PRELAUNCH))
            {
                trans.arrivaltime = Planetarium.GetUniversalTime() + prepT + TtfLO;
            }
            else //More working code
            {
                double distance = _central.GetDistanceBetweenPoints(trans.VesselFrom.protoVessel.latitude, trans.VesselFrom.protoVessel.longitude, trans.VesselTo.protoVessel.latitude, trans.VesselTo.protoVessel.longitude);
                trans.arrivaltime = Planetarium.GetUniversalTime() + prepT + (distance * TpD);
            }

        }

        //go to previous entry in vessel list for this body
        public void previousBodyVesselList(ref int ListPosition)
        {
            if (ListPosition <= 0)
            {
                ListPosition = _central.bodyVesselList.Count - 1;

            }
            else
            {
                ListPosition = ListPosition - 1;
            }
        }

        //go to next entry in vessel list for this body
        public void nextBodyVesselList(ref int ListPosition)
        {
            if (ListPosition >= _central.bodyVesselList.Count - 1)
            {
                ListPosition = 0;
            }
            else
            {
                ListPosition = ListPosition + 1;
            }
        }
    }
}