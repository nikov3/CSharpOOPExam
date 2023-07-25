using RobotService.Core.Contracts;
using RobotService.Models;
using RobotService.Models.Contracts;
using RobotService.Repositories;
using RobotService.Utilities.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RobotService.Core
{
    public class Controller : IController
    {
        private SupplementRepository supplements;
        private RobotRepository robots;

        public Controller()
        {
            supplements= new SupplementRepository();
            robots= new RobotRepository();
        }
        public string CreateRobot(string model, string typeName)
        {
            if (typeName != "DomesticAssistant" && typeName != "IndustrialAssistant")
            {
                return string.Format(OutputMessages.RobotCannotBeCreated, typeName);
            }
            if (typeName == "DomesticAssistant")
            {
                IRobot robot = new DomesticAssistant(model);
                robots.AddNew(robot);
            }
            if (typeName == "IndustrialAssistant")
            {
                IRobot robot = new IndustrialAssistant(model);
                robots.AddNew(robot);
            }
            return string.Format(OutputMessages.RobotCreatedSuccessfully, typeName, model);


        }

        public string CreateSupplement(string typeName)
        {
            if (typeName != "SpecializedArm" && typeName != "LaserRadar")
            {
                return string.Format(OutputMessages.SupplementCannotBeCreated, typeName);
            }
            if (typeName == "SpecializedArm")
            {
                ISupplement sup = new SpecializedArm();
                supplements.AddNew(sup);
            }
            if (typeName == "LaserRadar")
            {
                ISupplement sup = new LaserRadar();
                supplements.AddNew(sup);
            }
            return string.Format(OutputMessages.SupplementCreatedSuccessfully, typeName);

        }

        public string UpgradeRobot(string model, string supplementTypeName)
        {
            ISupplement sup = supplements.Models().FirstOrDefault(s => s.GetType().Name == supplementTypeName);
            int interfaceValue = sup.InterfaceStandard;
            List<IRobot> listOfR = robots.Models().Where(r => r.InterfaceStandards.All(x => x != interfaceValue)).ToList();
            listOfR = listOfR.Where(r => r.Model == model).ToList();
            if (listOfR.Count == 0)
            {
                return string.Format(OutputMessages.AllModelsUpgraded, model);
            }
            listOfR[0].InstallSupplement(sup);
            supplements.RemoveByName(supplementTypeName);
            return string.Format(OutputMessages.UpgradeSuccessful, model, supplementTypeName);
        }

        public string PerformService(string serviceName, int intefaceStandard, int totalPowerNeeded)
        {
            List<IRobot> listOfR = robots.Models().Where(r => r.InterfaceStandards.Any(x => x == intefaceStandard)).ToList();
            if (listOfR.Count == 0)
            {
                return string.Format(OutputMessages.UnableToPerform, intefaceStandard);
            }
            listOfR = listOfR.OrderByDescending(r => r.BatteryLevel).ToList();
            int sum = 0;
            foreach (IRobot robot in listOfR)
            {
                sum += robot.BatteryLevel;
            }
            if (sum < totalPowerNeeded)
            {
                return string.Format(OutputMessages.MorePowerNeeded, serviceName, totalPowerNeeded - sum);
            }
            int counter = 0;
            if (sum >= totalPowerNeeded)
            {
                foreach (IRobot robot in listOfR)
                {
                    if (robot.BatteryLevel >= totalPowerNeeded)
                    {
                        robot.ExecuteService(totalPowerNeeded);
                        counter++;
                        break;
                    }
                    else
                    {
                        totalPowerNeeded -= robot.BatteryLevel;
                        robot.ExecuteService(robot.BatteryLevel);
                        counter++;
                    }
                }
            }
            return string.Format(OutputMessages.PerformedSuccessfully, serviceName, counter);
        }

        //public string RobotRecovery(string model, int minutes)
        //{
        //    List<IRobot> listOfR = robots.Models().Where(r => r.BatteryLevel < r.BatteryCapacity * 0.5).ToList();
        //    foreach (IRobot robot in listOfR)
        //    {
        //        robot.Eating(minutes);
        //    }
        //    return string.Format(OutputMessages.RobotsFed, listOfR.Count);
        //}
        public string RobotRecovery(string model, int minutes)
        {
            int robotsFed = 0;
            List<IRobot> listOfR = robots.Models().Where(r => r.BatteryLevel < r.BatteryCapacity * 0.5).ToList();
            foreach (IRobot robot in listOfR)
            {
                if (robot.Model == model) 
                {
                    robot.Eating(minutes);
                    robotsFed++;
                }

            }
            return string.Format(OutputMessages.RobotsFed, robotsFed);
        }


        public string Report()
        {
            List<IRobot> listOfR = robots.Models().ToList();
            listOfR = listOfR.OrderByDescending(r => r.BatteryLevel).ThenBy(r => r.BatteryCapacity).ToList();
            StringBuilder sb = new StringBuilder();
            foreach (IRobot robot in listOfR)
            {
                sb.AppendLine(robot.ToString());
            }
            return sb.ToString().TrimEnd();
        }
    }
}
