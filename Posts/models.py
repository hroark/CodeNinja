import datetime
from enum import Enum
from django.db import models
from django.contrib.auth.models import User

class postType(Enum):
    request = 1
    code = 2



class basePost(models.Model):
    title = models.CharField(max_length=50)
    text = models.CharField(max_length=250)
    createDate = models.DateTimeField("Date created")
    language = models.CharField(max_length=10)
    author = models.ManyToManyField(User)
    pType = postType
    
    def __str__(self) -> str:
        return self.title

class requestPost(basePost):
    pType = postType.request
    
    def __init__(self,*args,**kwargs):
        super(basePost, self).__init__(*args,**kwargs)
    
    def __str__(self) -> str:
        return super().__str__()





class codePost(basePost):
    ninjaPoints = models.IntegerField(default=0)
    pType = postType.code
    
    def __init__(self,*args,**kwargs):
        super(basePost, self).__init__(*args,**kwargs)
    
    def __str__(self) -> str:
        return super().__str__()
    
    
