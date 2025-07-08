using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using Scholarly.DataAccess;
using Scholarly.Entity;
using Scholarly.WebAPI.Helper;
using Scholarly.WebAPI.Model;
using System.Net;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace Scholarly.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PDFController : ControllerBase
    {
        private readonly SWBDBContext _swbDBContext;
        private readonly IConfiguration _config;
        private readonly IPDFHelper _PDFHelper;
        public CurrentContext _currentContext;
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        public PDFController(IConfiguration configuration, SWBDBContext swbDBContext, IPDFHelper pDFHelper, IHttpContextAccessor httpContextAccessor)
        {
            _config = configuration;
            _swbDBContext = swbDBContext;
            _PDFHelper = pDFHelper;
            _currentContext = Common.GetCurrentContext(httpContextAccessor.HttpContext.User.Identity as ClaimsIdentity);
        }

        [HttpPost]
        [Route("addgroup")]
        public ActionResult AddGroup(string UserId, string GroupName, string TagsText)
        {
            bool flag = false;

            try
            {
                if ((
                    from x in _swbDBContext.tbl_groups
                    where x.group_name == GroupName && x.created_by == UserId
                    select x.group_name).Count<string>() != 0)
                {
                    flag = false;
                }
                else
                {
                    string[] strArrays = TagsText.Split(new char[] { ',' });

                    tbl_groups tBLGroup = new tbl_groups()
                    {
                        user_id = UserId,
                        group_name = GroupName,
                        created_by = UserId,
                        created_date = DateTime.UtcNow,
                        updated_date = DateTime.UtcNow,
                    };
                    _swbDBContext.tbl_groups.Add(tBLGroup);
                    _swbDBContext.SaveChanges();
                    int groupID = tBLGroup.group_id;
                    string[] strArrays1 = strArrays;
                    for (int i = 0; i < (int)strArrays1.Length; i++)
                    {
                        string str = strArrays1[i];
                        var tBLGroupsEmail = new tbl_groups_emails()
                        {
                            user_id = UserId,
                            email = str,
                            created_by = UserId,
                            created_date = DateTime.UtcNow,
                            updated_date = DateTime.UtcNow,
                            group_id = new int?(groupID)
                        };
                        _swbDBContext.tbl_groups_emails.Add(tBLGroupsEmail);
                        _swbDBContext.SaveChanges();
                        flag = true;
                    }

                }
            }
            catch (Exception exception)
            {
                throw exception;
            }
            return Ok(flag);
        }

        [HttpGet]
        [Route("loadgroups")]
        public ActionResult LoadGroups(string UserId)
        {
            List<Groups> groups = new List<Groups>();
            try
            {
                groups = (
                    from x in (
                        from q in _swbDBContext.tbl_groups
                        where q.status != (bool?)true && q.user_id == UserId
                        select new Groups()
                        {
                            GroupId = (int?)q.group_id,
                            GroupName = q.group_name,
                            Members = (int?)_swbDBContext.tbl_groups_emails.Where<tbl_groups_emails>((tbl_groups_emails x) => x.group_id == (int?)q.group_id && x.status != (bool?)true).Count<tbl_groups_emails>(),
                            Groupmails = _swbDBContext.tbl_groups_emails.Where<tbl_groups_emails>((tbl_groups_emails x) => x.group_id == (int?)q.group_id && x.status != (bool?)true).Select<tbl_groups_emails, GroupEmails>((tbl_groups_emails a) => new GroupEmails()
                            {
                                Email = a.email,
                                GroupEmailId = (int?)a.group_email_id
                            }).ToList<GroupEmails>()
                        }).ToList<Groups>()
                    where x.GroupName != ""
                    select x).ToList<Groups>();
            }
            catch (Exception exception)
            {
                throw exception;
            }
            return Ok(groups);
        }

        [HttpPost]
        [Route("addnewmail")]
        public ActionResult Addnewemail(string UserId, string newEmail, int GroupId)
        {
            bool flag = false;
            try
            {

                var tBLGroupsEmail = new tbl_groups_emails()
                {
                    user_id = UserId,
                    email = newEmail,
                    created_by = UserId,
                    created_date = DateTime.UtcNow,
                    updated_date = DateTime.UtcNow,
                    group_id = new int?(GroupId)
                };
                _swbDBContext.tbl_groups_emails.Add(tBLGroupsEmail);
                _swbDBContext.SaveChanges();
                flag = true;
            }
            catch (Exception exception)
            {
                return Ok(exception.Message);
            }
            return Ok(flag);
        }
        [HttpGet]
        [Route("contactlistpdf2")]
        public ActionResult ContactListPDF2()
        {
            ActionResult fileContentResult;
            try
            {
                byte[] file = _PDFHelper.GetFile(_PDFHelper.GetFilePath(3, _swbDBContext));
                if (file == null)
                {
                    fileContentResult = null;
                }
                else
                {
                    fileContentResult = new FileContentResult(file, "application/pdf");
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                fileContentResult = null;
            }
            return fileContentResult;
        }

        [HttpGet]
        [Route("getpdfpath")]
        public ActionResult GetPDFPath(int? PathId)
        {

            PDF? pDF = new PDF();
            try
            {
                pDF = (
                    from x in _swbDBContext.tbl_pdf_uploads
                    where (int?)x.pdf_uploaded_id == PathId
                    select x into q
                    select new PDF()
                    {
                        PDFPath = q.pdf_saved_path,
                        IsAccessed = (q.is_public == (bool?)true ? "Open Access" : "Closed Access")
                    }).FirstOrDefault<PDF>();
            }
            catch (Exception exception)
            {
                throw exception;
            }
            return Ok(pDF);

            //JsonResult nullable = base.Json(pDF, JsonRequestBehavior.AllowGet);
            //nullable.MaxJsonLength = new int?(2147483647);
            //return nullable;
        }
        [HttpPost]
        [Route("deleteemail")]
        public ActionResult deleteemail(string UserId, int GroupEmailId)
        {
            bool flag = false;
            try
            {

                tbl_groups_emails? nullable = (
                    from x in _swbDBContext.tbl_groups_emails
                    where x.group_email_id == GroupEmailId
                    select x).FirstOrDefault<tbl_groups_emails>();
                if (nullable != null)
                {
                    nullable.status = new bool?(true);
                    nullable.updated_by = UserId;
                    nullable.updated_date = DateTime.UtcNow;
                    _swbDBContext.SaveChanges();
                }
            }
            catch (Exception exception)
            {
                throw exception;
            }
            return Ok(flag);
        }

        [HttpPost]
        [Route("deletegroup")]
        public ActionResult deleteGroup(string UserId, int GroupId)
        {
            bool flag = false;
            try
            {

                tbl_groups? nullable = (
                    from x in _swbDBContext.tbl_groups
                    where x.group_id == GroupId
                    select x).FirstOrDefault<tbl_groups>();
                if (nullable != null)
                {
                    nullable.status = new bool?(true);
                    nullable.updated_by = UserId;
                    nullable.updated_date = DateTime.UtcNow;
                    _swbDBContext.SaveChanges();
                    flag = true;
                }
            }
            catch (Exception exception)
            {
                throw exception;
            }
            return Ok(flag);
        }

        [HttpPost]
        [Route("deletepdf")]
        public ActionResult DeletePdf(int UId)
        {
            bool flag = false;
            try
            {

                tbl_pdf_uploads? nullable = (
                    from x in _swbDBContext.tbl_pdf_uploads
                    where x.pdf_uploaded_id == UId
                    select x).FirstOrDefault<tbl_pdf_uploads>();
                if (nullable != null)
                {
                    nullable.status = new bool?(true);
                    _swbDBContext.SaveChanges();
                    flag = true;
                }

            }
            catch (Exception exception)
            {
                throw exception;
            }
            return Ok(flag);
        }

        [HttpPost]
        [Route("deletequestion")]
        public ActionResult DeleteQuestion(int QID)
        {
            bool flag = false;
            try
            {
                tbl_pdf_question_tags? result = _swbDBContext.tbl_pdf_question_tags.FirstOrDefault(x => x.question_id == QID);
                if (result != null)
                {
                    result.isdeleted = new bool?(true);
                    _swbDBContext.SaveChanges();
                    flag = true;
                }


            }
            catch (Exception exception)
            {
                throw exception;
            }
            return Ok(flag);
        }
        [HttpGet]
        [Route("editpdf")]
        public IActionResult EditPdf(int UId)
        {
            JsonResult jsonResult;
            tbl_pdf_uploads? result = null;
            PDF pDF = new PDF(); ;
            try
            {
                result = _swbDBContext.tbl_pdf_uploads.FirstOrDefault(p => p.pdf_uploaded_id == UId);
                if (result != null)
                {
                    pDF = new PDF();
                    pDF.PDFUploadedId = (int?)result.pdf_uploaded_id;
                    pDF.DOINo = result.doi_number;
                    pDF.PUBMEDId = result.pub_med_id;
                    pDF.Article = result.article;
                    pDF.Author = result.author;


                }

            }
            catch (Exception exception)
            {
                throw exception;
            }
            return Ok(pDF);
        }
        [HttpGet]
        [Route("downloadpdf")]
        public string DownloadPdf(string downloadLink, string storageLink)
        {
            string str;
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    webClient.DownloadFile(downloadLink, storageLink);
                }
                str = storageLink;
            }
            catch (Exception exception)
            {
                throw exception;
            }
            return str;
        }

        [HttpPost]
        [Route("savefile")]
        public ActionResult SaveUploadedFile([FromForm] FileDetail formval) //,[FromBody],
        {
            ActionResult action;
            string result = "";
            string str = "";
            string str1 = "";
            List<PDF> pDFs = new List<PDF>();
            try
            {
                if (formval.url == "" && formval.file == null)
                {
                    str = "Please Select File or Enter Url.";
                }
                if (formval.article == "")
                {
                    str1 = "Please Enter Article Name.";
                }
                string str2 = string.Format("{0} \\n {1}", str, str1);
                if (!(str == "") || !(str1 == ""))
                {
                    result = str2;
                }
                else if (!string.IsNullOrWhiteSpace(formval.url))
                {
                    string str3 = Helper.Common.CreateDownloadFolders(_config.GetSection("AppSettings")["DownloadFolderPath"], _logger);
                    string str4 = string.Concat(formval.article, ".pdf");
                    string str5 = this.DownloadPdf(formval.url, Path.Combine(str3, str4)); //""; 

                    var tBLPDFUPLOAD = new tbl_pdf_uploads()
                    {
                        user_id = _currentContext.UserId.ToString(),
                        pdf_saved_path = str5,
                        pub_med_id = formval.pubmedid,
                        article = formval.article,
                        author = formval.author,
                        created_by = "1",
                        created_date = DateTime.UtcNow,
                        file_name = formval.author,
                        doi_number = formval.doi,
                        is_public = new bool?(false)
                    };
                    _swbDBContext.tbl_pdf_uploads.Add(tBLPDFUPLOAD);
                    _swbDBContext.SaveChanges();

                    return Ok("File Uploaded Successfully");
                }
                else if (formval.file != null && formval.file.Length > 0)
                {
                    string fileName = Path.GetFileName(formval.file.FileName);
                    string str6 = Helper.Common.CreateDownloadFolders(_config.GetSection("AppSettings")["DownloadFolderPath"], _logger); ;
                    string str7 = fileName;
                    if (str7.Substring(str7.Length - 3, 3) != "pdf")
                    {
                        result = "File Upload Failed, Select only PDF format files";
                    }
                    else
                    {
                        if (Directory.Exists(str6))
                        {
                            using (var fileContentStream = new MemoryStream())
                            {
                                formval.file.CopyToAsync(fileContentStream);
                                fileContentStream.Position = 0; // Rewind!
                                System.IO.File.WriteAllBytes(Path.Combine(str6, fileName), fileContentStream.ToArray());
                            }

                            var tBLPDFUPLOAD1 = new tbl_pdf_uploads()
                            {
                                user_id = _currentContext.UserId.ToString(),
                                pdf_saved_path = Path.Combine(str6, fileName),
                                pub_med_id = formval.pubmedid,
                                doi_number = formval.doi,
                                article = formval.article,
                                author = formval.author,
                                created_by = "1",
                                created_date = DateTime.UtcNow,
                                file_name = fileName,
                                is_public=true,
                                status=true
                            };
                            _swbDBContext.tbl_pdf_uploads.Add(tBLPDFUPLOAD1);
                            _swbDBContext.SaveChanges();
                        }
                        else
                        {
                            Directory.CreateDirectory(str6);
                        }
                        result = "File Uploaded Successfully";
                    }
                }

            }
            catch (Exception exception)
            {
                result = "false";

            }
            return Ok(result);
        }
    }
}
