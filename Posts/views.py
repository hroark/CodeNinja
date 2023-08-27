from contextlib import contextmanager
from http.client import HTTPResponse
from pipes import Template
from turtle import title
from urllib.request import Request
from django.shortcuts import render
from django.http import HttpResponse
from django.template import Context, loader
from Posts.models import basePost, requestPost,codePost
from django.views import View
import logging
logger = logging.getLogger(__name__)


def index(request):
    requestList = requestPost.objects.order_by("createDate")[:5]
    template = loader.get_template('posts/index.html/')
    ctx={"requestList":requestList}

    return render(request,'posts/index.html/',ctx)
    

##index page
    #USer Stuff
    #Nav BAr
    #List of last 5 request and code posts
