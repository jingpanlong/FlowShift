import json,os,sys,subprocess,zipfile,shutil
from pathlib import Path
from PIL import Image
from pypdf import PdfReader
from docx import Document
from openpyxl import Workbook,load_workbook
from pptx import Presentation
from pptx.util import Inches
from reportlab.pdfgen import canvas

root=Path(__file__).resolve().parent.parent/'.build';root.mkdir(exist_ok=True)
engine=Path(sys.argv[1]) if len(sys.argv)>1 else root/'core-publish/FlowShift.CoreHost.exe'
base=root/'matrix';base.mkdir(exist_ok=True);fixtures=base/'fixtures';fixtures.mkdir(exist_ok=True)
env=os.environ.copy();env['FLOWSHIFT_DATA']=str(base/'data')
im=Image.new('RGB',(160,90),(78,129,220));im.save(fixtures/'photo.png')
(fixtures/'note.txt').write_text('FlowShift conversion test.\nHello world.',encoding='utf8')
(fixtures/'readme.md').write_text('# FlowShift\n\n**Hello**, conversion.\n\n|Name|Value|\n|---|---|\n|A|42|',encoding='utf8')
(fixtures/'page.html').write_text('<html><body><h1>FlowShift</h1><p>Hello <b>world</b>.</p></body></html>',encoding='utf8')
(fixtures/'shape.svg').write_text('<svg xmlns="http://www.w3.org/2000/svg" width="120" height="80"><rect width="120" height="80" fill="#407ce8"/><circle cx="60" cy="40" r="25" fill="white"/></svg>')
(fixtures/'data.json').write_text('[{"name":"Alice","score":42},{"name":"Bob","score":27}]')
(fixtures/'table.csv').write_text('name,score\nAlice,42\nBob,27\n')
(fixtures/'captions.srt').write_text('1\n00:00:00,000 --> 00:00:01,500\nHello FlowShift\n\n')
(fixtures/'map.geojson').write_text('{"type":"FeatureCollection","features":[{"type":"Feature","properties":{"name":"Shanghai"},"geometry":{"type":"Point","coordinates":[121.47,31.23]}}]}')
(fixtures/'mesh.obj').write_text('o Triangle\nv 0 0 0\nv 1 0 0\nv 0 1 0\nf 1 2 3\n')
(fixtures/'drawing.dxf').write_text('0\nSECTION\n2\nHEADER\n9\n$ACADVER\n1\nAC1015\n0\nENDSEC\n0\nSECTION\n2\nENTITIES\n0\nLINE\n5\n1\n100\nAcDbEntity\n8\n0\n100\nAcDbLine\n10\n0.0\n20\n0.0\n30\n0.0\n11\n100.0\n21\n100.0\n31\n0.0\n0\nENDSEC\n0\nEOF\n')
d=Document();d.add_heading('FlowShift',0);d.add_paragraph('Hello world. This is a real Office document.');d.save(fixtures/'report.docx')
w=Workbook();w.active.append(['Name','Score']);w.active.append(['Alice',42]);w.active.append(['Bob',27]);w.save(fixtures/'sheet.xlsx')
p=Presentation();s=p.slides.add_slide(p.slide_layouts[1]);s.shapes.title.text='FlowShift';s.placeholders[1].text='Hello conversion';p.save(fixtures/'slides.pptx')
pdf=canvas.Canvas(str(fixtures/'paper.pdf'));pdf.drawString(50,750,'FlowShift PDF text test.');pdf.showPage();pdf.drawString(50,750,'Second page.');pdf.save()
font=Path(__file__).resolve().parent.parent/'Vendor/Filee/tests/Filee.Engines.Tests/Fonts/FileeTest-TrueType.ttf';shutil.copyfile(font,fixtures/'sample.ttf')
with zipfile.ZipFile(fixtures/'archive.zip','w') as z:z.writestr('folder/hello.txt','Hello archive')
with zipfile.ZipFile(fixtures/'book.epub','w') as z:
 z.writestr('mimetype','application/epub+zip');z.writestr('META-INF/container.xml','<?xml version="1.0"?><container version="1.0" xmlns="urn:oasis:names:tc:opendocument:xmlns:container"><rootfiles><rootfile full-path="content.opf" media-type="application/oebps-package+xml"/></rootfiles></container>')
 z.writestr('content.opf','<package xmlns="http://www.idpf.org/2007/opf" version="2.0" unique-identifier="id"><metadata xmlns:dc="http://purl.org/dc/elements/1.1/"><dc:title>FlowShift</dc:title><dc:identifier id="id">flowshift-test</dc:identifier><dc:language>en</dc:language></metadata><manifest><item id="chapter" href="chapter.xhtml" media-type="application/xhtml+xml"/></manifest><spine><itemref idref="chapter"/></spine></package>')
 z.writestr('chapter.xhtml','<html xmlns="http://www.w3.org/1999/xhtml"><head><title>Chapter</title></head><body><h1>FlowShift</h1><p>Hello ebook.</p></body></html>')
cases=[('photo.png','webp'),('photo.png','avif'),('photo.png','jxl'),('photo.png','ico'),('photo.png','pdf'),('shape.svg','png'),('shape.svg','pdf'),('paper.pdf','png'),('paper.pdf','docx'),('paper.pdf','txt'),('paper.pdf','pptx'),('report.docx','pdf'),('report.docx','html'),('report.docx','txt'),('sheet.xlsx','csv'),('sheet.xlsx','json'),('sheet.xlsx','pdf'),('slides.pptx','pdf'),('slides.pptx','png'),('readme.md','docx'),('readme.md','pdf'),('page.html','docx'),('table.csv','xlsx'),('data.json','yaml'),('data.json','xml'),('data.json','csv'),('captions.srt','vtt'),('captions.srt','ass'),('archive.zip','7z'),('archive.zip','folder'),('sample.ttf','woff'),('sample.ttf','woff2'),('sample.ttf','otf'),('drawing.dxf','svg'),('drawing.dxf','dwg'),('mesh.obj','stl'),('mesh.obj','glb'),('map.geojson','kml'),('map.geojson','csv'),('book.epub','docx'),('book.epub','pdf'),('note.txt','zip')]
results=[]
def run(files,target,extra=None,label=None):
 idx=len(results)+1;out=base/f'case-{idx:03}';out.mkdir(exist_ok=True);req=base/f'request-{idx:03}.json';data={'Files':[str(fixtures/f) if isinstance(f,str) else str(f) for f in files],'Target':target,'Output':str(out),'Quality':90};data.update(extra or {});req.write_text(json.dumps(data),encoding='utf8')
 proc=subprocess.run([str(engine),'convert',str(req)],env=env,capture_output=True,text=True,encoding='utf8',timeout=150)
 resultpath=Path(str(req)+'.result.json');result=json.loads(resultpath.read_text(encoding='utf-8-sig')) if resultpath.exists() else {'Files':[]}
 paths=[Path(p) for f in result['Files'] for p in f.get('Outputs',[])]
 ok=proc.returncode==0 and bool(paths) and all(p.exists() for p in paths)
 if ok:
  for path in paths:
   if path.suffix.lower() in ('.png','.jpg','.webp','.ico'):Image.open(path).verify()
   if path.suffix.lower()=='.pdf':assert len(PdfReader(path).pages)>0
   if path.suffix.lower() in ('.docx','.pptx','.xlsx','.epub','.zip'):assert zipfile.is_zipfile(path)
 name=label or '+'.join(str(f) for f in files)+' -> '+target
 row={'test':name,'pass':ok,'result':result,'stderr':proc.stderr[-1400:]};results.append(row);print(('PASS ' if ok else 'FAIL ')+name,flush=True)
 return paths
for src,target in cases:run([src],target)
run(['paper.pdf'],'pdf',{'Pages':'2','Compress':True},'PDF page range and compression')
run(['paper.pdf','paper.pdf'],'pdf',{'Merge':True},'PDF merge batch')
run(['note.txt','photo.png'],'zip',{'Combine':True},'mixed file batch ZIP')
run(['photo.png'],'png',{'MaxWidth':40,'Grayscale':True,'Rotate':90,'Dpi':144,'StripMetadata':True},'image recipe resize gray rotate DPI')
run(['paper.pdf'],'pdf',{'Split':True},'PDF split pages')
run(['paper.pdf'],'png',{'LongImage':True},'PDF pages to long image')
hwpx=run(['report.docx'],'hwpx',label='DOCX to HWPX')
if hwpx:
 run([hwpx[0]],'pdf',label='HWPX to PDF with bundled rhwp')
 run([hwpx[0]],'docx',label='HWPX to DOCX')
run(['map.geojson'],'shp',label='GeoJSON to Shapefile with sidecar files')
ffmpeg=engine.parent/'engines/ffmpeg/bin/ffmpeg.exe'
if not ffmpeg.exists():ffmpeg=engine.parent/'engines/ffmpeg/ffmpeg.exe'
if ffmpeg.exists():
 subprocess.run([str(ffmpeg),'-v','error','-y','-f','lavfi','-i','sine=frequency=440:duration=0.5',str(fixtures/'tone.wav')],check=True)
 subprocess.run([str(ffmpeg),'-v','error','-y','-f','lavfi','-i','color=c=blue:s=160x90:d=0.5','-c:v','libx264','-pix_fmt','yuv420p',str(fixtures/'clip.mp4')],check=True)
 for f in ['mp3','flac','m4a','opus']:run(['tone.wav'],f)
 for f in ['webm','gif','png']:run(['clip.mp4'],f)
 run(['clip.mp4'],'jpg',{'Frames':True},'video all frames to JPG folder')
(base/'results.json').write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding='utf8')
passed=sum(r['pass'] for r in results);print(f'{passed}/{len(results)} passed',flush=True)
sys.exit(0 if passed==len(results) else 1)
