using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ExoLoader
{
    public class CustomJobData
    {
        public string ID;
        public string name;
        public string battleHeaderText;
        public Location location;
        public bool isRelax;
        public Skill primarySkill;
        public List<SkillChange> skillChanges;
        public SkillChange ultimateBonus;

        public void MakeJob()
        {
            ModInstance.log("Making job " + ID);
            Job newJob = new Job(ID, name, location, null, isRelax);
            newJob.primarySkill = primarySkill;
            newJob.skillChanges = skillChanges;
            newJob.ultimateSkillChanges = new List<SkillChange> { ultimateBonus };
            if (battleHeaderText != null )
            {
                TextLocalized text = new TextLocalized("job_doing_" + ID);
                text.AddLocale(Locale.EN, battleHeaderText);
            }
            ModInstance.log("Made job " + ID);
        }
    }
}
