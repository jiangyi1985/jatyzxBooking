﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace JatyzxBooking
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string ASPNETCOOKIE = "ASP.NET_SessionId=xxxxxxxxxxxxxxxxxxxxxx";

        //https://developers.weixin.qq.com/doc/offiaccount/OA_Web_Apps/Wechat_webpage_authorization.html
        private const string appid = "wx1d9019feb23fe2d1";
        private const string scope = "snsapi_base";

        HttpClient client;
        List<CourtDto> courtList;
        List<TimeDto> timeList;

        private const string uriHome = "http://www.jatyzx.com/szw/home/index";
        private const string uriGetMyInfo = "http://www.jatyzx.com/Common/EmpApi/ApiGetEmpInfo";
        private const string uriBadmintonHome = "http://www.jatyzx.com/SZW/VenueBill/Badminton";
        private const string uriBadmintonBookingPage = "http://www.jatyzx.com/SZW/VenueBill/VenueBill?VenueType=87D8B898-A13E-4C96-8913-D7E49E591021&VenueIcon=131";
        private const string uriGetCourtList = "http://www.jatyzx.com/SZW/VenueBill/ApiGetVenueList";
        private const string uriGetTimeList = "http://www.jatyzx.com/SZW/VenueBill/ApiGetVenueTimeList";
        private const string uriGetCourtStatus = "http://www.jatyzx.com/SZW/VenueBill/ApiGetVenueStatus";
        private const string uriCreateOrder = "http://www.jatyzx.com/SZW/VenueBill/ApiVenueBill";
        private const string uriGetOrderConfirmPage = "http://www.jatyzx.com/SZW/VenueBill/VenueBillConfirm?RecordNo=WX2021080600136";
        private const string uriGetOrderInfo = "http://www.jatyzx.com/SZW/Bill/ApiGetOrderInfo";
        private const string uriGetMyWalletInfo = "http://www.jatyzx.com/SZW/VenueBill/ApiGetVenueBillPos";
        private const string uriConfirmOrder = "http://www.jatyzx.com/SZW/VenueBill/ApiVenueBillOK";
        private const string uriGetMyOrderPage = "http://www.jatyzx.com/SZW/Bill/Order";
        private const string uriGetMyOrderList = "http://www.jatyzx.com/SZW/Bill/ApiGetOrder";
        private const string uriGetMyTransactionPage = "http://www.jatyzx.com/SZW/Bill/EmpInOut";
        private const string uriGetMyTransaction = "http://www.jatyzx.com/SZW/Bill/ApiGetEmpInOut";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            var httpClientHandler = new HttpClientHandler();
            httpClientHandler.AllowAutoRedirect = true;
            //client.DefaultRequestHeaders.Add("Cookie", txtCookie.Text.Trim());

            txtCookie.Text = ASPNETCOOKIE;
            calCourtDate.SelectedDate = DateTime.Today;

            client = new HttpClient(httpClientHandler);

            //await LoadCourtDefinition();
        }

        private async Task LoadCourtDefinition()
        {
            txtLog.AppendText("Getting court list..." + Environment.NewLine);
            courtList = await GetCourtList();
            foreach (var court in courtList)
            {
                txtLog.AppendText(court.ToString() + Environment.NewLine);
            }

            txtLog.AppendText(Environment.NewLine);

            txtLog.AppendText("Getting time list..." + Environment.NewLine);
            timeList = await GetTimeList();
            foreach (var time in timeList)
            {
                txtLog.AppendText(time.ToString() + Environment.NewLine);
            }

            txtLog.AppendText(Environment.NewLine);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            btnGetCourtStatus.IsEnabled = false;

            try
            {
                await DoGetCourtStatus(txtCourtDate.Text.Trim());
            }
            catch (Exception exception)
            {
                txtLog.AppendText(exception.Message + Environment.NewLine + exception.StackTrace + Environment.NewLine);
            }

            btnGetCourtStatus.IsEnabled = true;
        }

        private async Task DoGetCourtStatus(string courtDate = null)
        {
            if (!client.DefaultRequestHeaders.Contains("Cookie"))
                client.DefaultRequestHeaders.Add("Cookie", txtCookie.Text.Trim());

            

            if (string.IsNullOrEmpty(courtDate))
            {
                var date = DateTime.Today;
                for (int i = 0; i < 5; i++)
                {
                    var dateString = date.ToString("yyyy-MM-dd");
                    txtLog.AppendText("Getting court status " + dateString + " ..." + Environment.NewLine);
                    var courtStatusList = await GetCourtStatus(dateString);
                    PrintCourtTable(courtList, timeList, courtStatusList);
                    txtLog.AppendText(Environment.NewLine);

                    date = date.AddDays(1);
                }
            }
            else
            {
                txtLog.AppendText("Getting court status " + courtDate + " ..." + Environment.NewLine);
                var courtStatusList = await GetCourtStatus(courtDate);
                PrintCourtTable(courtList, timeList, courtStatusList);
                txtLog.AppendText(Environment.NewLine);
            }
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            btnCreateOrder.IsEnabled = false;

            try
            {
                var court = courtList.FirstOrDefault(c => c.VenueName == cbbVenue.SelectedItem.ToString());
                var startTime = timeList.FirstOrDefault(t => t.TimeStartName == cbbStartTime.Text);
                await DoCreateOrder(txtCourtDate.Text.Trim(),court.SysID,startTime.StartTime);
            }
            catch (Exception exception)
            {
                txtLog.AppendText(exception.Message + Environment.NewLine + exception.StackTrace + Environment.NewLine);
            }

            btnCreateOrder.IsEnabled = true;
        }

        private async Task DoCreateOrder(string courtDate, string venueId, int startTime)
        {
            if (!client.DefaultRequestHeaders.Contains("Cookie"))
                client.DefaultRequestHeaders.Add("Cookie", txtCookie.Text.Trim());

            //var orderId = await CreateOrder("2021-08-09", new List<BookItemDto>()
            //{
            //    new BookItemDto()
            //    {
            //        BillMoney = 25, BillTime = 30, StartTime = 720, Venue = "274e6a8e-0404-47c8-b530-02e10e0ce21f"
            //    },
            //    new BookItemDto()
            //    {
            //        BillMoney = 25, BillTime = 30, StartTime = 750, Venue = "274e6a8e-0404-47c8-b530-02e10e0ce21f"
            //    }
            //});

            //var orderId = await CreateOrder("2021-08-10", new List<BookItemDto>()
            //{
            //    new BookItemDto()
            //    {
            //        BillMoney = 35, BillTime = 30, StartTime = 1140, Venue = "42b6e4e7-664c-442f-b3c7-ba47ff8db90b"
            //    },
            //    new BookItemDto()
            //    {
            //        BillMoney = 35, BillTime = 30, StartTime = 1170, Venue = "42b6e4e7-664c-442f-b3c7-ba47ff8db90b"
            //    }
            //});

            var bookItemList = new List<BookItemDto>();
            for (int i = 0; i < 4; i++)
            {
                bookItemList.Add(new BookItemDto()
                {
                    BillMoney = 35,//todo: get this from order status info
                    BillTime = 30,
                    StartTime = startTime+i*30,
                    Venue = venueId
                });
            }

            var orderId = await CreateOrder(courtDate, bookItemList);

            if (orderId != null)
            {
                var orderInfo = await GetOrderInfo(orderId);

                var walletInfo = await GetMyWalletInfo(orderId);

                var success = await ConfirmOrder(orderId, walletInfo.Details.FirstOrDefault().SysApp,
                    (int)orderInfo.PayMoney);
            }
        }

        private void PrintCourtTable(List<CourtDto> courtList, List<TimeDto> timeList, List<CourseStatusDto> courtStatusList)
        {
            if (courtStatusList.Any(o => o.BillTime != 30))
            {
                txtLog.AppendText("courtStatusList.Any(o => o.BillTime != 30)" + Environment.NewLine);
            }

            var groupByTime = courtStatusList.GroupBy(s => s.StartTime);

            var first = groupByTime.FirstOrDefault();
            var strVenueIds = first.Select(o => o.Venue).Aggregate((o, n) => o + "," + n);

            if (groupByTime.Any(g => g.Select(o => o.Venue).Aggregate((o, n) => o + "," + n) != strVenueIds))
            {
                txtLog.AppendText(
                    "groupByTime.Any(g => g.Select(o => o.Venue).Aggregate((o, n) => o + \",\" + n) != strVenueIds)" +
                    Environment.NewLine);
            }

            txtLog.AppendText("    " + "\t" + "          " + "\t"
                              + first.Select(o => courtList.FirstOrDefault(c => c.SysID == o.Venue).VenueName)
                                  .Aggregate((o, n) => o + "\t" + n) + Environment.NewLine);

            foreach (var grouping in courtStatusList.GroupBy(s => s.StartTime))
            {
                txtLog.AppendText(grouping.Key + "\t" +
                                  timeList.FirstOrDefault(t => t.StartTime == grouping.Key).TimeStartEndName + "\t"
                                  + grouping.Select(o => o.IsFree ? o.Price.ToString("00.00") : "--.--")
                                      .Aggregate((o, n) => o + "\t" + n) + Environment.NewLine);
            }
        }

        private async Task<List<CourtDto>> GetCourtList()
        {
            var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, new Uri(uriGetCourtList))
            {
                Content = new FormUrlEncodedContent(new KeyValuePair<string, string>[]
                {
                    new("VenueType", "87D8B898-A13E-4C96-8913-D7E49E591021")
                })
            });
            var s = await response.Content.ReadAsStringAsync();

            var data = JsonSerializer.Deserialize<Result>(s).data;
            return JsonSerializer.Deserialize<List<CourtDto>>(data);
        }

        private async Task<List<TimeDto>> GetTimeList()
        {
            var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, new Uri(uriGetTimeList))
            {
                Content = new FormUrlEncodedContent(new KeyValuePair<string, string>[]
                {
                    new("VenueType", "87D8B898-A13E-4C96-8913-D7E49E591021")
                })
            });
            var s = await response.Content.ReadAsStringAsync();

            var data = JsonSerializer.Deserialize<Result>(s).data;
            return JsonSerializer.Deserialize<List<TimeDto>>(data);
        }

        private async Task<List<CourseStatusDto>> GetCourtStatus(string dateString)
        {
            var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, new Uri(uriGetCourtStatus))
            {
                Content = new FormUrlEncodedContent(new KeyValuePair<string, string>[]
                {
                    new("VenueType", "87D8B898-A13E-4C96-8913-D7E49E591021"),//badminton type guid?
                    new("BillDay", dateString)
                })
            });
            var s = await response.Content.ReadAsStringAsync();

            var data = JsonSerializer.Deserialize<Result>(s).data;

            if (data == null)
                txtLog.AppendText(s + Environment.NewLine);

            return data == null ? null : JsonSerializer.Deserialize<List<CourseStatusDto>>(data);
        }

        private async Task<string> CreateOrder(string dateString, List<BookItemDto> items)
        {
            var dict = new Dictionary<string, string>();
            dict.Add("VenueIcon", "131");
            dict.Add("BillDay", dateString);
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                dict.Add($"Details[{i}][Venue]", item.Venue);
                dict.Add($"Details[{i}][StartTime]", item.StartTime.ToString());
                dict.Add($"Details[{i}][BillTime]", item.BillTime.ToString());
                dict.Add($"Details[{i}][BillMoney]", item.BillMoney.ToString());
            }

            var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, new Uri(uriCreateOrder))
            {
                Content = new FormUrlEncodedContent(dict)
            });

            txtLog.AppendText(response.StatusCode + Environment.NewLine);

            var s = await response.Content.ReadAsStringAsync();

            txtLog.AppendText(s + Environment.NewLine);

            if (response.StatusCode != HttpStatusCode.OK) return null;

            var data = JsonSerializer.Deserialize<Result>(s).data;
            return data;
        }

        private async Task<OrderInfoDto> GetOrderInfo(string orderId)
        {
            var dict = new Dictionary<string, string>();
            dict.Add("RecordNo", orderId);

            var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, new Uri(uriGetOrderInfo))
            {
                Content = new FormUrlEncodedContent(dict)
            });

            txtLog.AppendText(response.StatusCode + Environment.NewLine);

            var s = await response.Content.ReadAsStringAsync();

            txtLog.AppendText(s + Environment.NewLine);

            if (response.StatusCode != HttpStatusCode.OK) return null;

            var data = JsonSerializer.Deserialize<Result>(s).data;
            return data == null ? null : JsonSerializer.Deserialize<OrderInfoDto>(data);
        }

        private async Task<WalletInfoDto> GetMyWalletInfo(string orderId)
        {
            var dict = new Dictionary<string, string>();
            dict.Add("RecordNo", orderId);//不传的话 server 5xx

            var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, new Uri(uriGetMyWalletInfo))
            {
                Content = new FormUrlEncodedContent(dict)
            });

            txtLog.AppendText(response.StatusCode + Environment.NewLine);

            var s = await response.Content.ReadAsStringAsync();

            txtLog.AppendText(s + Environment.NewLine);

            if (response.StatusCode != HttpStatusCode.OK) return null;

            var data = JsonSerializer.Deserialize<Result>(s).data;
            return data == null ? null : JsonSerializer.Deserialize<WalletInfoDto>(data);
        }

        private async Task<bool> ConfirmOrder(string orderId, string walletItemId, int orderPrice)
        {
            var dict = new Dictionary<string, string>();
            dict.Add("RecordNo", orderId);//不传的话 server 5xx
            dict.Add("SysApp", walletItemId);
            dict.Add("BillValue", orderPrice.ToString());

            var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, new Uri(uriConfirmOrder))
            {
                Content = new FormUrlEncodedContent(dict)
            });
            var s = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<Result>(s);
            return result.code == "0" && result.msg == "success";
        }

        private void txtCourtDate_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            calCourtDate.Visibility = Visibility.Visible;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            calCourtDate.Visibility = Visibility.Visible;
        }

        private void calCourtDate_DisplayDateChanged(object sender, CalendarDateChangedEventArgs e)
        {

        }

        private void calCourtDate_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            txtCourtDate.Text = (e.Source as Calendar).SelectedDate.Value.ToString("yyyy-MM-dd");
            calCourtDate.Visibility = Visibility.Hidden;
        }

        private async void Button_Click_3(object sender, RoutedEventArgs e)
        {
            await LoadCourtDefinition();

            foreach (var courtDto in courtList)
            {
                cbbVenue.Items.Add(courtDto.VenueName);
            }

            foreach (var timeDto in timeList)
            {
                cbbStartTime.Items.Add(timeDto.TimeStartName);
            }

            btnGetCourtStatus.IsEnabled = true;
            btnCreateOrder.IsEnabled = true;
        }

        private void txtCourtDate_TextChanged(object sender, TextChangedEventArgs e)
        {
            //calCourtDate.Visibility = Visibility.Visible;
        }
    }

    public class CourtDto
    {
        public string SysID { get; set; }
        public string VenueName { get; set; }

        public override string ToString()
        {
            return VenueName + "\t" + SysID;
        }
    }

    public class TimeDto
    {
        public int StartTime { get; set; }
        public int BillTime { get; set; }
        public string TimeStartName { get; set; }
        public string TimeEndName { get; set; }
        public string TimeStartEndName { get; set; }

        public override string ToString()
        {
            return StartTime + "\t" + BillTime + " " + TimeStartName + " " + TimeEndName + " " + TimeStartEndName;
        }
    }

    public class CourseStatusDto
    {
        public int StartTime { get; set; }
        public int BillTime { get; set; }
        public string Venue { get; set; }
        /// <summary>
        /// is available
        /// </summary>
        public bool IsFree { get; set; }
        public decimal Price { get; set; }

        public override string ToString()
        {
            return StartTime + "\t" + BillTime + " " + Venue + " " + IsFree + " " + Price;
        }
    }

    public class BookItemDto
    {
        public string Venue { get; set; }
        public int StartTime { get; set; }
        public int BillTime { get; set; }
        public int BillMoney { get; set; }
    }

    public class OrderInfoDto
    {
        public string RecordNo { get; set; }
        public string BillTime { get; set; }
        public decimal PayMoney { get; set; }
        public string BillType { get; set; }
        public string BillStatus { get; set; }//10:新建 未支付
        public List<OrderItemDto> Details { get; set; }
        public bool CanDel { get; set; }
        public bool CanUnBook { get; set; }
        public int VenueCount { get; set; }
    }
    public class OrderItemDto
    {
        public string VenueName { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string BillDay { get; set; }
        public string BillTime { get; set; }
    }

    public class WalletInfoDto
    {
        public string EmpInfo { get; set; }
        public bool IsWXEmp { get; set; }
        public List<WalletItemDto> Details { get; set; }
    }
    public class WalletItemDto
    {
        public bool CanUse { get; set; }
        public string ErrMsg { get; set; }
        public string AppName { get; set; }
        public string SysApp { get; set; }
        public decimal PosValue { get; set; }
        public decimal CurrValue { get; set; }
        public string EndDay { get; set; }
    }
    public class Result
    {
        public string code { get; set; }
        public string msg { get; set; }
        public string data { get; set; }
    }
}