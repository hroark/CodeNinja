from django.shortcuts import render
from django.http import HttpResponse
from django.template import loader
from .models import basePost, requestPost,codePost


def index(request):
    requestList = requestPost.objects.order_by("createDate")[:5]
    codeList = codePost.objects.order_by("createDate")[:5]
    template = loader.get_template('posts/index.html/')
    context = {"Lastest Requests": requestList, "Latest Code": codeList}
    return HttpResponse(template.render(context,request))
    
     

##index page
    #USer Stuff
    #Nav BAr
    #List of last 5 request and code posts
