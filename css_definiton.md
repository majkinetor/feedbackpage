This is the deafult CSS style which is hardcoded in the FeedbackPage template. Its compact version is delivered with each page with configurable prefix that is added to the CSS identities (via `prefix` attribute in web.config or `FeedbackPrefix` class property). You can use custom css by specifying `style` option in Feedback configuration section. Customize the default css given bellow and add it in the config section.

Keep in mind that if you use special XML characters inside style, for instance ">", one of the CSS relationsheep selectors, you need to enclose the entire string in `<![CDATA[` and `]]>` tags.

```
<style>
          #body {
	          width: [config.width]; 
	          position: absolute; 
          }
          #caption {
	          background: #fff8dc;
	          border:1px #bbbbbb solid;
	          padding: 3px;
	          overflow: hidden;
          }
          #title {
	          font-weight: bold;
	          float: left;
	          background:transparent;
	          width:85%;
          }
          #icons {
	          text-align: right;
	          background:transparent;
          }
          #icons  a {
	          padding-right: 10px; 
	          text-decoration: none;
          }
          #header {
	          width: 100%;
	          padding-top: 10px;		
          }
          #form{
	          display: inline;
          }
          #text {
	          width: 98%; 
	          height: 300px;	 
	          border: 1px;
	          background: #fffff4;
          }
          #file {
	          width: 100%;
          }
          #submit {
	          margin-top: 10px;
	          text-align: center;	
	          width:99%;
	          border: 1px solid;
          }                         
</style>
```