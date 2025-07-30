import { getMockPdfs } from "../../mockPdfs";

export async function GET(req) {
  const { searchParams } = new URL(req.url);
  const userId = searchParams.get("userId")
  const keyword = searchParams.get("keyword");
  const authToken = req.headers.get("Authorization");

  if (!authToken) {
    return new Response(JSON.stringify({ error: "Unauthorized: No token provided" }), {
      status: 401,
      headers: { "Content-Type": "application/json" },
    });
  }

  if (!userId) {
    return Response.json({ success: false, message: 'Missing userId' }, { status: 400 });
  }

  if (!keyword) {
    return Response.json({ success: false, message: 'Missing keyword' }, { status: 400 });
  }

  const allPdfs = getMockPdfs();
  const pdfList = allPdfs.filter((pdf) => pdf.userId !== userId)

  const filteredPdfs = pdfList.filter((pdf) => {
    const lowerKeyword = keyword.toLowerCase(); 
    return (
      pdf.article?.toLowerCase().includes(lowerKeyword) ||
      pdf.pubmedid?.toLowerCase().includes(lowerKeyword) ||
      pdf.author?.toLowerCase().includes(lowerKeyword) ||
      pdf.doi?.toLowerCase().includes(lowerKeyword)
    );
  });

  return Response.json({ success: true, data: filteredPdfs });
}
