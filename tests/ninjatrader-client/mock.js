const http=require('http'); const hits={}; const log=[];
const s=http.createServer((q,r)=>{const c=[];q.on('data',d=>c.push(d));q.on('end',()=>{hits[q.url]=(hits[q.url]||0)+1;log.push({url:q.url,ct:q.headers['content-type'],ua:q.headers['user-agent'],body:Buffer.concat(c).toString()});
 if(q.url==='/hits'){r.end(JSON.stringify({hits,log}));return;}
 if(q.url==='/401'){r.writeHead(401);r.end();return;}
 if(q.url==='/flaky'&&hits[q.url]===1){r.writeHead(503);r.end();return;}
 if(q.url==='/hang')return; r.writeHead(200);r.end('{"ok":true}');});});
s.listen(+process.argv[2],'127.0.0.1');
