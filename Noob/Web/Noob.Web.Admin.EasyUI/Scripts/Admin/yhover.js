

//------------------------�������������ʾͼƬ����һ��-----------------------------------------------------------
//--------ʹ�÷�����Begin---------
////ͼƬ��ʽ��
//function imgFormatter(value, row, index) {
//    //return '<img src="../../img/adv.gif" border="0" style="cursor:pointer;width:18px;height:16px;"  alt="' + rootimg + '/AD/20140428135818702.jpg"/>';
//    var html = '';
//    var arr = value.split('@');
//    if (arr && arr.length > 0) {
//        for (var i = 0; i < arr.length; i++) {
//            if (arr[i] != null && arr[i] != '') {
//                html += '<img src="../../img/adv.gif" border="0" style="cursor:pointer;width:18px;height:16px;"  alt="' + rootimg + '/' + row.Account + '/GroupPraise/' + arr[i] + '"/>';
//            }
//        }
//    }
//    return html;
//}
//--------ʹ�÷�����end---------
var XYHover = function () {
	var D = document, ua = navigator.userAgent.toLowerCase(), isOpera = (ua.indexOf('opera') > -1), isIE = (!isOpera && ua.indexOf('msie') > -1);

	return {
	    images: function (ID, TagName) {
	    var Container = (typeof ID == "string")? D.getElementById(ID):D, TName = Container.getElementsByTagName(TagName), i, len = TName.length, Overlayer = null, oSelf = this;
			for (i = 0; i < len; ++i) {
				TName[i].onmouseover = function(){
				var imgPath = this.alt, imgAlt = this.alt, bigImg = '<img src="' + imgPath + '" alt="' + imgAlt + '" />', pageX = oSelf.getPageX(this), pageY = oSelf.getPageY(this), viewportWidth = oSelf.getViewportWidth(), viewportHeight = oSelf.getViewportHeight(), layerWidth = 0, layerHeight = 0, xScroll = oSelf.getXScroll(), yScroll = oSelf.getYScroll();
					if (!Overlayer) {
						Overlayer = D.createElement('div');
						Overlayer.id = 'overlayer';
						D.body.appendChild(Overlayer);
					}
					else {
						Overlayer.style.display = 'block';
					}
					layerWidth = Overlayer.offsetWidth;
					layerHeight = Overlayer.offsetHeight;
					if ((pageX + this.offsetWidth + 5 + layerWidth) > (viewportWidth + xScroll)) {
						pageX -= 5 + layerWidth;
					}
					else {
						pageX += this.offsetWidth + 5;
					}
					if ((pageY + this.offsetHeight + 5 + layerHeight) > (viewportHeight + yScroll)) {
						pageY -= 5 + layerHeight;
					}
					Overlayer.style.left = pageX + 'px';
					Overlayer.style.top = pageY + 'px';
					Overlayer.innerHTML = bigImg;
				};
				TName[i].onmouseout = function(){
					Overlayer.style.display = 'none';
					Overlayer.innerHTML = '';
				};
			}
		},
		getXScroll: function(){
			var j = self.pageXOffset || D.documentElement.scrollLeft || D.body.scrollLeft;
			return j;
		},
		getYScroll: function(){
			var j = self.pageYOffset || D.documentElement.scrollTop || D.body.scrollTop;
			return j;
		},
		getViewportWidth: function(){
			var j = self.innerWidth;
			var k = D.compatMode;
			if (k || isIE) {
				j = (k == "CSS1Compat") ? D.documentElement.clientWidth : D.body.clientWidth;
			}
			return j;
		},
		getViewportHeight: function(){
			var j = self.innerHeight;
			var k = D.compatMode;
			if ((k || isIE) && !isOpera) {
				j = (k == "CSS1Compat") ? D.documentElement.clientHeight : D.body.clientHeight;
			}
			return j;
		},
		getPageX: function(el){
			var box = null, parentNode = null, left = 0;
			if (el.getBoundingClientRect) {
				box = el.getBoundingClientRect();
				left = box.left + Math.max(D.documentElement.scrollLeft, D.body.scrollLeft);
			}
			else {
				left = el.offsetLeft;
				parentNode = el.offsetParent;
				if (parentNode != el) {
					while (parentNode) {
						left += parentNode.offsetLeft;
						parentNode = parentNode.offsetParent;
					}
				}
			}
			return left;
		},
		getPageY: function(el){
			var box = null, parentNode = null, top = 0;
			if (el.getBoundingClientRect) {
				box = el.getBoundingClientRect();
				top = box.top + Math.max(D.documentElement.scrollTop, D.body.scrollTop);
			}
			else {
				alert('x');
				top = el.offsetTop;
				parentNode = el.offsetParent;
				if (parentNode != el) {
					while (parentNode) {
						top += parentNode.offsetTop;
						parentNode = parentNode.offsetParent;
					}
				}
			}
			return top;
		}
	};
}();




//------------------------�������������ʾͼƬ��������-------------------------------------------------------------
/*�����������������ʾͼƬ�������Ϣ��������--------Begin---------,by michael in 2014/8/29          */
//--------ʹ�÷�����Begin---------
////ͼƬ��ʽ��
//function imgFormatter(value, row, index) {
//    //return '<img src="../../img/adv.gif" border="0" style="cursor:pointer;width:18px;height:16px;"  alt="' + rootimg + '/AD/20140428135818702.jpg"/>';
//    var title = 'http://192.168.16.115:8088/UserImage//012345/GroupPraise/20140820164044338.JPEG';
//    return '<a class="screenshot" href="javascript:void(0)" title="ͼƬ��ַ�� ' + title + '" rel="' + title + '"><img src="../../img/adv.gif" border="0" style="cursor:pointer;width:18px;height:16px;" /></a>';
//}

//����
//var opt = null;       //{"border": "1px solid #ccc", "background": "#333", "color": "#fff", "fontSize": "9px", "width": "300", "height": "200","xOffset": "10", "yOffset": "30", "tempOffset": "65"};
//screenshotPreview(opt);
//--------ʹ�÷�����end---------

this.screenshotPreview = function (opt) {
    // these 2 variable determine popup's distance from the cursor
    // you might want to adjust to get the right result
    var xOffset = 10, yOffset = 30;
    var fontSize = "9px";  //�趨Ĭ�����������С
    var border = "1px solid #ccc";  //�趨Ĭ�����������С
    var background = "#333";  //�趨Ĭ����������ɫ
    var padding = "1px";  //�趨Ĭ������padding
    var color = "#fff";  //�趨Ĭ��������ɫ
    var tempOffset = "65";  //�趨ͼƬ��ʾƫ����

    var width = "200";  //�趨ָ��ͼƬ��ʾ���
    var height = "200";  //�趨ָ��ͼƬ��ʾ�߶�

    //Ĭ�ϲ���
    var defaultOptions = {
        "position": "absolute", "display": "none", "padding": padding,
        "border": border, "color": color, "fontSize": fontSize
        , "width": width, "height": height, "xOffset": xOffset, "yOffset": yOffset, "tempOffset": tempOffset
    };
    $.extend(defaultOptions, opt);

    $("a.screenshot").hover(function (e) {
        //ָ����ʾͼƬ����ʾλ��
        var top = e.pageY - xOffset;
        var left = e.pageX + yOffset;

        var winHeight = $(window).height();     //���ڸ߶�
        if (winHeight - top < defaultOptions.height) {
            top = e.pageY - (height - (winHeight - top)) - defaultOptions.tempOffset;
        }
        var winWidth = $(window).width();     //���ڸ߶�
        if (winWidth - left < defaultOptions.width) {
            left = e.pageX - yOffset - defaultOptions.width;
        }

        this.t = this.title;
        this.title = "";
        var c = (this.t != "") ? "<br/>" + this.t : "";
        $("body").append("<p id='screenshot'><img id='imgScreenShot' src='" + this.rel + "' alt=''/>" + c + "</p>");

        //��ʾ��
        var $screenshot = $("#screenshot");
        //��ȡ��ʾ��������С,���嵥λ
        var unit = $screenshot.css("font-size").slice(-2);
        //��ʾ����ʽ���
        $screenshot.css({
            "position": defaultOptions.position, "display": defaultOptions.display
            , "font-size": defaultOptions.fontSize, "border": defaultOptions.border, "background": defaultOptions.background
            , "padding": defaultOptions.padding, "color": defaultOptions.color
        });
        //��ʾ��λ����ʾ
        $screenshot.css({ "top": top + "px", "left": left + "px" }).fadeIn("fast");

        //ͼƬ��ʾ
        var $imgScreenShot = $("#imgScreenShot");
        //var img_w = $imgScreenShot.width();     //ͼƬ��� 
        //var img_h = $imgScreenShot.height();    //ͼƬ�߶� 
        //if (img_w > width) {    //���ͼƬ��ȳ����������--Ҫ������ 
        //   var height = (width * img_h) / img_w; //�߶ȵȱ����� 
        //}
        //else{
        //    width = img_w;
        //    height = img_h;
        //}
        $imgScreenShot.css({ "width": width + "px", "height": height + "px" });
    },
	function () {
	    this.title = this.t;
	    $("#imgScreenShot").remove();
	    $("#screenshot").remove();
	});

    $("a.screenshot").mousemove(function (e) {
        //ָ����ʾͼƬ����ʾλ��
        var top = e.pageY - defaultOptions.xOffset;
        var left = e.pageX + defaultOptions.yOffset;
        //���ڸ߶�
        var winHeight = $(window).height();
        if (winHeight - top < defaultOptions.height) {
            top = e.pageY - (defaultOptions.height - (winHeight - top)) - defaultOptions.tempOffset;
        }
        var winWidth = $(window).width();     //���ڸ߶�
        if (winWidth - left < defaultOptions.width) {
            left = e.pageX - yOffset - defaultOptions.width;
        }
        $("#screenshot").css({ "top": top + "px", "left": left + "px" });
    });
};
/*�����������������ʾͼƬ�������Ϣ��������--------end---------,by michael in 2014/8/25          */
