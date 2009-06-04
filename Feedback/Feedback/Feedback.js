var Feedback = {      
        init : function( prefix, dockpos, bShow ) {    
            
            if (bShow == null) bShow = true;
            this.body    = document.getElementById( prefix + 'body');
            this.caption = document.getElementById( prefix + 'caption');
            this.header  = document.getElementById( prefix + 'header');
            this.content = document.getElementById( prefix + 'content');
            this.text    = document.getElementById( prefix + 'text');
            this.file    = document.getElementById( prefix + 'file');
            this.dockpos = dockpos;
            

            this.setdim();    
            this.body.style.display = bShow ? '' : 'none';
            this.dock(this.dockpos);
            this.movable();
            document.body.onresize = Feedback.resize;
        
        },
        
        toggle : function() {
            var bVisible = this.content.style.display != 'none';         
            
            this.content.style.display = bVisible ? 'none' : '';
            this.header.style.display = bVisible ? 'none' : '';  
            
            if (!bVisible) this.text.focus();          
            this.resize();
        },
        
        hide : function () { this.body.style.display = 'none'; },         
        show : function () { this.body.style.display = '';}, 
        
        dock : function(location) {

            switch(location.substring(0,1)) {
			    case 'L':  this.body.style.left = 0; break;
			    case 'M':  this.body.style.left = Math.floor((this.windowWidth - this.body.offsetWidth)/2) + 'px'; break;
			    case 'R':  this.body.style.left = (this.windowWidth - this.body.offsetWidth) + 'px'; break;
		    }
		    switch(location.substring(1)) {
			    case 'T':  this.body.style.top = 0; break;
			    case 'M':  this.body.style.top = Math.floor((this.windowHeight - this.body.offsetHeight)/2) + 'px'; break;
			    case 'B':  this.body.style.top = (this.windowHeight - this.body.offsetHeight) + 'px'; break;
		    }				
		}, 
		
		setdim : function() {
			this.windowHeight = window.innerHeight || document.documentElement.clientHeight;
		    this.windowWidth = window.innerWidth || document.documentElement.clientWidth;
		},
				
		movable: function() {
            Drag.init(this.caption, this.body, 0, this.windowWidth - this.body.offsetWidth, 0, this.windowHeight - this.body.offsetHeight);
		},
		
		resize: function() {
		    Feedback.setdim();	
		    var t=Feedback;	    
		    Drag.init(t.caption, t.body, 0, t.windowWidth - t.body.offsetWidth, 0, t.windowHeight - t.body.offsetHeight);
		    t.dock(t.dockpos);
		},	
		           
        finish: function(response) {
                if (response != "OK") { Feedback.content.innerHTML = response; return; }
                
                Feedback.toggle(); 
                Feedback.text.value = "";  
                Feedback.file.outerHTML = Feedback.file.outerHTML;
        }
}; 
AIM = {
 
	frame : function(c) {
 
		var n = 'f' + Math.floor(Math.random() * 99999);
		var d = document.createElement('DIV');
		d.innerHTML = '<iframe style="display:none" src="about:blank" id="'+n+'" name="'+n+'" onload="AIM.loaded(\''+n+'\')"></iframe>';
		document.body.appendChild(d);
 
		var i = document.getElementById(n);
		if (c && typeof(c.onComplete) == 'function') {
			i.onComplete = c.onComplete;
		}
 
		return n;
	},
 
	form : function(f, name) {
		f.setAttribute('target', name);
	},
 
	submit : function(f, c) {
		AIM.form(f, AIM.frame(c));
		if (c && typeof(c.onStart) == 'function') {
			return c.onStart();
		} else {
			return true;
		}
	},
 
	loaded : function(id) {
		var i = document.getElementById(id);
		if (i.contentDocument) {
			var d = i.contentDocument;
		} else if (i.contentWindow) {
			var d = i.contentWindow.document;
		} else {
			var d = window.frames[id].document;
		}
		if (d.location.href == "about:blank") {
			return;
		}
 
		if (typeof(i.onComplete) == 'function') {
			i.onComplete(d.body.innerHTML);
		}
	}
 
}
var Drag = {

	obj : null,

	init : function(o, oRoot, minX, maxX, minY, maxY, bSwapHorzRef, bSwapVertRef, fXMapper, fYMapper)
	{
		o.onmousedown	= this.start;

		o.hmode			= bSwapHorzRef ? false : true ;
		o.vmode			= bSwapVertRef ? false : true ;

		o.root = oRoot && oRoot != null ? oRoot : o ;

		if (o.hmode  && isNaN(parseInt(o.root.style.left  ))) o.root.style.left   = "0px";
		if (o.vmode  && isNaN(parseInt(o.root.style.top   ))) o.root.style.top    = "0px";
		if (!o.hmode && isNaN(parseInt(o.root.style.right ))) o.root.style.right  = "0px";
		if (!o.vmode && isNaN(parseInt(o.root.style.bottom))) o.root.style.bottom = "0px";

		o.minX	= typeof minX != 'undefined' ? minX : null;
		o.minY	= typeof minY != 'undefined' ? minY : null;
		o.maxX	= typeof maxX != 'undefined' ? maxX : null;
		o.maxY	= typeof maxY != 'undefined' ? maxY : null;

		o.xMapper = fXMapper ? fXMapper : null;
		o.yMapper = fYMapper ? fYMapper : null;

		o.root.onDragStart	= new Function();
		o.root.onDragEnd	= new Function();
		o.root.onDrag		= new Function();
	},

	start : function(e)
	{
		var o = Drag.obj = this;
		e = Drag.fixE(e);
		var y = parseInt(o.vmode ? o.root.style.top  : o.root.style.bottom);
		var x = parseInt(o.hmode ? o.root.style.left : o.root.style.right );
		o.root.onDragStart(x, y);

		o.lastMouseX	= e.clientX;
		o.lastMouseY	= e.clientY;

		if (o.hmode) {
			if (o.minX != null)	o.minMouseX	= e.clientX - x + o.minX;
			if (o.maxX != null)	o.maxMouseX	= o.minMouseX + o.maxX - o.minX;
		} else {
			if (o.minX != null) o.maxMouseX = -o.minX + e.clientX + x;
			if (o.maxX != null) o.minMouseX = -o.maxX + e.clientX + x;
		}

		if (o.vmode) {
			if (o.minY != null)	o.minMouseY	= e.clientY - y + o.minY;
			if (o.maxY != null)	o.maxMouseY	= o.minMouseY + o.maxY - o.minY;
		} else {
			if (o.minY != null) o.maxMouseY = -o.minY + e.clientY + y;
			if (o.maxY != null) o.minMouseY = -o.maxY + e.clientY + y;
		}

		document.onmousemove	= Drag.drag;
		document.onmouseup		= Drag.end;

		return false;
	},

	drag : function(e)
	{
		e = Drag.fixE(e);
		var o = Drag.obj;

		var ey	= e.clientY;
		var ex	= e.clientX;
		var y = parseInt(o.vmode ? o.root.style.top  : o.root.style.bottom);
		var x = parseInt(o.hmode ? o.root.style.left : o.root.style.right );
		var nx, ny;

		if (o.minX != null) ex = o.hmode ? Math.max(ex, o.minMouseX) : Math.min(ex, o.maxMouseX);
		if (o.maxX != null) ex = o.hmode ? Math.min(ex, o.maxMouseX) : Math.max(ex, o.minMouseX);
		if (o.minY != null) ey = o.vmode ? Math.max(ey, o.minMouseY) : Math.min(ey, o.maxMouseY);
		if (o.maxY != null) ey = o.vmode ? Math.min(ey, o.maxMouseY) : Math.max(ey, o.minMouseY);

		nx = x + ((ex - o.lastMouseX) * (o.hmode ? 1 : -1));
		ny = y + ((ey - o.lastMouseY) * (o.vmode ? 1 : -1));

		if (o.xMapper)		nx = o.xMapper(y)
		else if (o.yMapper)	ny = o.yMapper(x)

		Drag.obj.root.style[o.hmode ? "left" : "right"] = nx + "px";
		Drag.obj.root.style[o.vmode ? "top" : "bottom"] = ny + "px";
		Drag.obj.lastMouseX	= ex;
		Drag.obj.lastMouseY	= ey;

		Drag.obj.root.onDrag(nx, ny);
		return false;
	},

	end : function()
	{
		document.onmousemove = null;
		document.onmouseup   = null;
		Drag.obj.root.onDragEnd(	parseInt(Drag.obj.root.style[Drag.obj.hmode ? "left" : "right"]), 
									parseInt(Drag.obj.root.style[Drag.obj.vmode ? "top" : "bottom"]));
		Drag.obj = null;
	},

	fixE : function(e)
	{
		if (typeof e == 'undefined') e = window.event;
		if (typeof e.layerX == 'undefined') e.layerX = e.offsetX;
		if (typeof e.layerY == 'undefined') e.layerY = e.offsetY;
		return e;
	}
};
