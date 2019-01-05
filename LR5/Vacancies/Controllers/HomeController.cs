﻿using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using RestSharp;
using Vacancies.Models;

namespace Vacancies.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            JobsViewMod model = GetVacancies(120000, 15000);
            return View(model);
        }
		
        private const string host = "https://api.hh.ru";
        private const string resource = "/vacancies";
        private const int vacanciesPerPage = 100;
        private const int firstPage = 0;

        private readonly IRestClient client = new RestClient(host);

        public JobsViewMod GetVacancies(int bigSalary, int lowSalary)
        {
            JobsViewMod model = new JobsViewMod(bigSalary, lowSalary);
            IRestResponse response = GetVacancies(firstPage);
            int pagesCount = (int)JObject.Parse(response.Content)["pages"];
            JArray vacancies = JObject.Parse(response.Content)["items"] as JArray;
            for (int i = firstPage; i < pagesCount; i++)
            {
                foreach (var vacancy in vacancies)
                {
                    if (vacancy["salary"].Type == JTokenType.Null)
                        continue;
                    var salaryFrom = vacancy["salary"]["from"];
                    var salaryTo = vacancy["salary"]["to"];
                    var salaryCurr = vacancy["salary"]["currency"];
                    var salaryFromType = salaryFrom.Type;
                    var salaryToType = salaryTo.Type;
                    double salary = -1;
                    if ((string)salaryCurr != "RUR")
                        continue;
                    else if (salaryFromType != JTokenType.Null && salaryToType != JTokenType.Null)
                        salary = ((double)salaryFrom + (double)salaryTo) / 2;
                    else if (salaryFromType == JTokenType.Null && salaryToType != JTokenType.Null)
                        salary = (double)salaryTo;
                    else if (salaryFromType != JTokenType.Null && salaryToType == JTokenType.Null)
                        salary = (double)salaryFrom;
                    if (salary >= bigSalary)
                    {
                        var profession = (string)vacancy["name"];
                        if (!model.ProfessionsBigSalary.Contains(profession))
                        {
                            model.ProfessionsBigSalary.Add(profession);
                        }
                        var details = JObject.Parse(GetVacancyDetails((string)vacancy["id"]).Content);
                        JArray keySkills = details["key_skills"] as JArray;
                        if (keySkills.HasValues)
                        {
                            foreach (var keySkill in keySkills)
                            {
                                var skill = (string)keySkill["name"];
                                if (model.SkillsBigSalary.Contains(skill))
                                    continue;
                                model.SkillsBigSalary.Add(skill);
                            }
                        }
                    }
                    else if (salary > 0 && salary < lowSalary)
                    {
                        var profession = (string)vacancy["name"];
                        if (!model.ProfessionsLowSalary.Contains(profession))
                        {
                            model.ProfessionsLowSalary.Add(profession);
                        }
                        var details = JObject.Parse(GetVacancyDetails((string)vacancy["id"]).Content);
                        JArray keySkills = details["key_skills"] as JArray;
                        if (keySkills.HasValues)
                        {
                            foreach (var keySkill in keySkills)
                            {
                                var skill = (string)keySkill["name"];
                                if (model.SkillsLowSalary.Contains(skill))
                                    continue;
                                model.SkillsLowSalary.Add(skill);
                            }
                        }
                    }
                }
                response = GetVacancies(firstPage + i + 1);
                vacancies = JObject.Parse(response.Content)["items"] as JArray;
            }
            return model;
        }

        private IRestResponse GetVacancies(int page)
        {
            IRestRequest request = new RestRequest(string.Format("{0}?page={1}&per_page={2}", resource, page, vacanciesPerPage), Method.GET);
            request.AddParameter("only_with_salary", "true");
            return client.Execute(request);
        }

        private IRestResponse GetVacancyDetails(string id)
        {
            IRestRequest request = new RestRequest(string.Format("{0}/{1}", resource, id), Method.GET);
            return client.Execute(request);
        }
    }
}
