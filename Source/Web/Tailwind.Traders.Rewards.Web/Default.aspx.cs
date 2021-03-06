﻿using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using Tailwind.Traders.Rewards.Web.data;

namespace Tailwind.Traders.Rewards.Web
{
    public partial class _Default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string yolo = Request.QueryString["yolo"];
            if (yolo == "hi")

            {
                throw new Exception();
            }

            var cid = -1;
            if (Page.IsPostBack)
            {
                cid = int.Parse(CustomerId.Value);
            }
            using (var ctx = new ContextDB())
            {
                var orders = ctx.Orders
                    .Take(5)
                    .ToList();

                orderList.DataSource = orders;
                orderList.DataBind();

                if (cid == -1)
                {
                    var defaultCustomer = ctx.Customers.FirstOrDefault();
                    MapCustomer(defaultCustomer);
                }
                else
                {
                    var customer = ctx.Customers.Single(c => c.CustomerId == cid);
                    MapCustomer(customer);
                }
            }

            lblCheckbox.Attributes.Add("for", EnrollCheckbox.ClientID);
        }

        protected void EnrollChckedChanged(object sender, EventArgs e)
        {
            var cid = int.Parse(CustomerId.Value);
            using (var ctx = new ContextDB())
            {
                var customer = ctx.Customers
                    .Where(c => c.CustomerId == cid)
                    .SingleOrDefault();

                if (customer.Enrrolled == EnrollmentStatusEnum.Uninitialized)
                {
                    customer.Enrrolled = EnrollmentStatusEnum.Started;
                    ctx.SaveChanges();
                    BypassLogicAppIfNeeded(customer);
                    MapCustomer(customer);
                }
            }
        }

        private void BypassLogicAppIfNeeded(Customer customer)
        {
            var bypassSettings = ConfigurationManager.AppSettings["ByPassLogicApp"];
            if (string.IsNullOrEmpty(bypassSettings) || !bool.Parse(bypassSettings))
            {
                return;
            }

            var uri = ConfigurationManager.AppSettings["AfHttpHandlerUri"];

            var client = new HttpClient();
            var obj = new
            {
                enrolled = customer.Enrrolled,
                firstName = customer.FirstName,
                id = customer.CustomerId,
                lastName = customer.LastName,
                mobileNumber = customer.MobileNumber
            };
            var json = JsonConvert.SerializeObject(obj);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var result = client.PostAsync(uri, content).Result;
        }

        protected void SearchCustomer(object sender, EventArgs e)
        {
            using (var ctx = new ContextDB())
            {
                var customer = ctx.Customers
                    .Where(c => c.Email.Contains(SearchTextBox.Text) || c.FirstName.Contains(SearchTextBox.Text))
                    .SingleOrDefault();

                if (customer != null)
                {
                    MapCustomer(customer);
                    spanCustomer.Visible = true;
                    spanCustomer2.Visible = true;
                    spanNotFound.Visible = false;
                }
                else
                {
                    spanCustomer.Visible = false;
                    spanCustomer2.Visible = false;
                    spanNotFound.Visible = true;
                }

                RandomizeOrderHistory(ctx);
            }

            SearchTextBox.Text = string.Empty;            
        }

        private void MapCustomer(Customer customer)
        {
            CustomerId.Value = customer.CustomerId.ToString();
            AccNo.InnerText = customer.AccountCode;
            Address1.InnerText = customer.FirstAddress;
            City.InnerText = customer.City;
            Country.InnerText = customer.Country;
            ZipCode.InnerText = customer.ZipCode;
            Website.InnerText = customer.Website;
            Active.InnerText = customer.Active ? "yes" : "no";
            Name.InnerText = customer.FirstName;
            LastName.InnerText = customer.LastName;
            Email.InnerText = customer.Email;
            PhoneNumber.InnerText = customer.PhoneNumber;
            FaxNumber.InnerText = customer.FaxNumber;
            MobileNumber.InnerText = customer.MobileNumber;
            switch (customer.Enrrolled)
            {
                case EnrollmentStatusEnum.Accepted:
                    EnrollCheckbox.Visible = false;
                    lblCheckbox.InnerText = "Already enrolled";
                    break;
                case EnrollmentStatusEnum.Rejected:
                    EnrollCheckbox.Visible = false;
                    lblCheckbox.InnerText = "Enrollment rejected";
                    break;
                case EnrollmentStatusEnum.Started:
                    EnrollCheckbox.Visible = true;
                    EnrollCheckbox.Enabled = false;
                    EnrollCheckbox.Checked = true;
                    lblCheckbox.InnerText = "Enrollment in process";
                    break;
                case EnrollmentStatusEnum.Uninitialized:
                case EnrollmentStatusEnum.Uncompleted:
                    EnrollCheckbox.Visible = true;
                    EnrollCheckbox.Enabled = true;
                    EnrollCheckbox.Checked = false;
                    lblCheckbox.InnerText = "Enroll in loyalty program";
                    break;
            }

        }

        private void RandomizeOrderHistory(ContextDB ctx)
        {
            var shuffledOrders = ctx.Orders
                    .OrderBy(a => Guid.NewGuid())
                    .Take(5)
                    .ToList();

            orderList.DataSource = shuffledOrders;
            orderList.DataBind();
        }
    }
}