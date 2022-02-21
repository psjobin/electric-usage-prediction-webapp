using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Mvc;

namespace PredictPowerUsage.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _config;

        public HomeController(ILogger<HomeController> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(string password, string myDate)
        {
            ViewData["lblErr"] = "";
            ViewData["lblPrediction"] = "";
            if (password == _config.GetValue<string>("password"))
            {

                if(DateTime.TryParse(myDate, out DateTime dateTime))
                {
                    if(dateTime.Year != 2018)
                    {
                        ViewData["lblErr"] = "Invalid year! - Only select a date in the year 2018";
                    }
                    else
                    {
                        AWS_class aws = new AWS_class(_config);
                        if (aws.UploadFile(myDate))
                        {
                            string prediction = System.IO.File.ReadAllText(_config.GetValue<string>("predictiontxtFilename"));
                            ViewData["lblPrediction"] = $"{prediction} Megawatts.";
                        }
                    }
                }
                else
                {
                    ViewData["lblErr"] = "Invalid year!";
                }
            }
            else
            {
                ViewData["lblErr"] = "Invalid Password";
            }
            return View();
        }
    }


    public class AWS_class
    {
        private readonly IConfiguration _config;
        public AWS_class(IConfiguration configuration)
        {
            _config = configuration;
        }

        internal bool UploadFile(string date)
        {
            bool result = true;

            try
            {
                var filepath = _config.GetValue<string>("AcsvDateFilename");
                string bucketname = _config.GetValue<string>("bucketname");
                using (FileStream fs = new FileStream(filepath, FileMode.Create, FileAccess.Write))
                {
                    using (StreamWriter writer = new StreamWriter(fs))
                    {
                        writer.WriteLine(date);
                    }
                }

                using (FileStream file = new FileStream(filepath, FileMode.Open, FileAccess.Read))
                {
                    using (var client = new AmazonS3Client(_config.GetValue<string>("awsAccessKeyId"), _config.GetValue<string>("awsSecretAccessKey"), RegionEndpoint.USEast1))
                    {
                        using (var newMemoryStream = new MemoryStream())
                        {
                            file.CopyTo(newMemoryStream);

                            var uploadRequest = new TransferUtilityUploadRequest
                            {
                                InputStream = newMemoryStream,
                                Key = filepath,
                                BucketName = bucketname
                            };

                            var fileTransferUtility = new TransferUtility(client);
                            fileTransferUtility.Upload(uploadRequest);
                        }

                        bool found = false;
                        while (!found)
                        {
                            try
                            {
                                var response = client.GetObjectMetadataAsync(new GetObjectMetadataRequest() { BucketName = bucketname, Key = _config.GetValue<string>("resulttxtFilename") }).Result;
                                found = true;
                            }

                            catch (Exception ex)
                            {
                                found = false;
                            }
                        }

                        var ftu = new TransferUtility(client);
                        ftu.Download(_config.GetValue<string>("resulttxtFilename"), bucketname, _config.GetValue<string>("resulttxtFilename"));

                        client.DeleteObjectAsync(new Amazon.S3.Model.DeleteObjectRequest() { BucketName = bucketname, Key = _config.GetValue<string>("resulttxtFilename") }).Wait();

                        //  2022-02-20T06:15:20.754Z
                    }
                }

                string dateAsNum = File.ReadAllText(_config.GetValue<string>("resulttxtFilename"));
                using (FileStream fs = new FileStream(filepath, FileMode.Create, FileAccess.Write))
                {
                    using (StreamWriter writer = new StreamWriter(fs))
                    {
                        writer.WriteLine(dateAsNum);
                    }
                }

                using (FileStream file = new FileStream(filepath, FileMode.Open, FileAccess.Read))
                {
                    using (var client = new AmazonS3Client(_config.GetValue<string>("awsAccessKeyId"), _config.GetValue<string>("awsSecretAccessKey"), RegionEndpoint.USEast1))
                    {
                        using (var newMemoryStream = new MemoryStream())
                        {
                            file.CopyTo(newMemoryStream);

                            var uploadRequest = new TransferUtilityUploadRequest
                            {
                                InputStream = newMemoryStream,
                                Key = filepath,
                                BucketName = _config.GetValue<string>("predictBucketName")
                            };

                            var fileTransferUtility = new TransferUtility(client);
                            fileTransferUtility.Upload(uploadRequest);
                        }

                        bool found = false;
                        while (!found)
                        {
                            try
                            {
                                var response = client.GetObjectMetadataAsync(new GetObjectMetadataRequest() { BucketName = _config.GetValue<string>("predictBucketName"), Key = _config.GetValue<string>("predictiontxtFilename") }).Result;
                                found = true;
                            }

                            catch (Exception ex)
                            {
                                found = false;
                            }
                        }

                        var ftu = new TransferUtility(client);
                        ftu.Download(_config.GetValue<string>("predictiontxtFilename"), _config.GetValue<string>("predictBucketName"), _config.GetValue<string>("predictiontxtFilename"));

                        client.DeleteObjectAsync(new Amazon.S3.Model.DeleteObjectRequest() { BucketName = _config.GetValue<string>("predictBucketName"), Key = _config.GetValue<string>("predictiontxtFilename") }).Wait();

                        //  2022-02-20T06:15:20.754Z
                    }

                }
            }
            catch (Exception ex)
            {
                result = false;
            }

            return result;
        }

        public bool DownloadFile()
        {
            bool result = true;
            return result;
        }
    }
}