using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NLog;
using SautinSoft;
using Scholarly.DataAccess;
using Scholarly.Entity;
using Scholarly.WebAPI.DataAccess;
using Scholarly.WebAPI.Helper;
using Scholarly.WebAPI.Model;
using System.Net;
using System.Security.Claims;

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
        private readonly IPdfDa _IPdfDa;
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        public PDFController(IConfiguration configuration, SWBDBContext swbDBContext, IPDFHelper pDFHelper, IPdfDa iPdfDa, IHttpContextAccessor httpContextAccessor)
        {
            _config = configuration;
            _swbDBContext = swbDBContext;
            _PDFHelper = pDFHelper;
            _IPdfDa = iPdfDa;
            _currentContext = Common.GetCurrentContext(httpContextAccessor.HttpContext.User.Identity as ClaimsIdentity);
        }

        [HttpPost]
        [Route("addgroup")]
        public ActionResult AddGroup(string UserId, string GroupName, string TagsText)
        {
            return Ok(_IPdfDa.AddGroup(_swbDBContext, _logger, UserId, GroupName, TagsText));
        }

        [HttpGet]
        [Route("loadgroups")]
        public ActionResult LoadGroups(string UserId)
        {

            return Ok(_IPdfDa.LoadGroups(_swbDBContext, _logger, UserId));
        }

        [HttpPost]
        [Route("addnewmail")]
        public ActionResult Addnewemail(string UserId, string newEmail, int GroupId)
        {

            return Ok(_IPdfDa.AddNewEmail(_swbDBContext, _logger, UserId, newEmail, GroupId));
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
        public ActionResult GetPDFPath(int PathId)
        {

            return Ok(_IPdfDa.GetPDFPath(_swbDBContext, _logger, PathId));

        }
        [HttpPost]
        [Route("deleteemail")]
        public ActionResult deleteemail(string UserId, int GroupEmailId)
        {
            return Ok(_IPdfDa.DeleteEmail(_swbDBContext, _logger, UserId, GroupEmailId));
        }

        [HttpPost]
        [Route("deletegroup")]
        public ActionResult deleteGroup(string UserId, int GroupId)
        {
            return Ok(_IPdfDa.DeleteGroup(_swbDBContext, _logger, UserId, GroupId));
        }

        [HttpPost]
        [Route("deletepdf")]
        public ActionResult DeletePdf(int UId)
        {
            return Ok(_IPdfDa.DeletePdf(_swbDBContext, _logger, UId));
        }

        [HttpPost]
        [Route("deletequestion")]
        public ActionResult DeleteQuestion(int QID)
        {
            return Ok(_IPdfDa.DeleteQuestion(_swbDBContext, _logger, QID));
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
                    string str5 = ""; //this.DownloadPdf(formval.url, Path.Combine(str3, str4));

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
                        is_public = new bool?(false),
                        status = true
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
                                file_name = fileName
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

        [HttpGet]
        [Route("getsearchvalues")]
        public ActionResult getsearchvalues(string searchtext, string loginuserId)
        {
            DbSet<tbl_pdf_uploads> tBLPDFUPLOADS = _swbDBContext.tbl_pdf_uploads;
            return Ok(tBLPDFUPLOADS);
        }


        [HttpGet]
        [Route("tunseencomment")]
        public ActionResult GetUnSeenComments(int QuestionId)
        {
            return Ok("Not yet implemented");
        }


        [HttpGet]
        [Route("uploadedpdfslist")]
        public ActionResult GetUploadedPDFsList()
        {
            List<PDF> pDFs = new List<PDF>();
            try
            {
                string str = _currentContext.UserId.ToString();
                pDFs = (
                    from P in _swbDBContext.tbl_pdf_uploads
                    where P.user_id == str && P.status == (bool?)true
                    select new PDF()
                    {
                        PDFPath = P.pdf_saved_path,
                        PDFUploadedId = (int?)P.pdf_uploaded_id,
                        CreatedDate = P.created_date,
                        FileName = P.file_name,
                        PUBMEDId = P.pub_med_id,
                        Article = P.article,
                        DOINo = P.doi_number,
                        IsAccessed = (P.is_public == (bool?)true ? "Open Access" : "Closed Access"),
                        Author = P.author,
                        Annotationscount =10,// P.tbl_pdf_question_tags.Where<tbl_pdf_question_tags>((tbl_pdf_question_tags x) => x.pdf_uploaded_id == (int?)P.pdf_uploaded_id && x.isdeleted != (bool?)true).Count<tbl_pdf_question_tags>(),
                        AnnotatedQuestions = _swbDBContext.tbl_pdf_question_tags.Where<tbl_pdf_question_tags>((tbl_pdf_question_tags x) => x.pdf_uploaded_id == (int?)P.pdf_uploaded_id).Select<tbl_pdf_question_tags, Questions>((tbl_pdf_question_tags q) => new Questions()
                        {
                            Question = q.question,
                            QuestionId = (int?)q.question_id,
                            likescount = (int?)_swbDBContext.tbl_annotation_ratings.Where<tbl_annotation_ratings>((tbl_annotation_ratings x) => x.question_id == (int?)q.question_id && x.is_liked == (bool?)true).Count<tbl_annotation_ratings>(),
                            dislikescount = (int?)_swbDBContext.tbl_annotation_ratings.Where<tbl_annotation_ratings>((tbl_annotation_ratings x) => x.question_id == (int?)q.question_id && x.is_liked == (bool?)false).Count<tbl_annotation_ratings>(),
                            Comments = _swbDBContext.tbl_comments.Where<tbl_comments>((tbl_comments x) => x.is_seen != (bool?)true && x.question_id == (int?)q.question_id).Select<tbl_comments, string>((tbl_comments x) => x.comment).FirstOrDefault<string>(),
                            CommentsCount = (int?)_swbDBContext.tbl_comments.Where<tbl_comments>((tbl_comments x) => x.is_seen != (bool?)true && x.question_id == (int?)q.question_id).Select<tbl_comments, string>((tbl_comments x) => x.comment).Count<string>()
                        })
                        .ToList<Questions>()
                    }).ToList<PDF>();
            }
            catch (Exception exception)
            {
                throw exception;
            }

            return Ok(pDFs);
        }

        [HttpGet]
        [Route("pdftohtml")]
        public ActionResult? PDFTOHTML(int? Path)
        {
            try
            {
                if(_swbDBContext.tbl_pdf_uploads.Any(p => p.pdf_uploaded_id == Path))
                {
                    var result = _swbDBContext.tbl_pdf_uploads.FirstOrDefault(p => p.pdf_uploaded_id == Path);
                    if (result != null && !string.IsNullOrEmpty(result.pdf_saved_path))
                    {
                        System.IO.Path.ChangeExtension(result.pdf_saved_path, ".html");
                        PdfFocus pdfFocu = new PdfFocus();
                        pdfFocu.HtmlOptions.IncludeImageInHtml = true;
                        pdfFocu.HtmlOptions.Title = "Simple text";
                        pdfFocu.OpenPdf(result.pdf_saved_path);
                        string html = "";
                        if (pdfFocu.PageCount > 0)
                        {
                            html = pdfFocu.ToHtml();
                        }
                        return base.Content(html);
                    }
                }
            }
            catch (Exception exception)
            {
                throw;
            }
            return null;
        }
    }

}

