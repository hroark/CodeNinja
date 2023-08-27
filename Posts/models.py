from enum import Enum
from pyexpat import model
from django.db import models
from django.contrib.auth.models import User

class postType(Enum):
    request = 1
    code = 2


class Language(models.Model):
    lang_name = models.CharField(max_length=10)
    def __str__(self):
        return f"{self.lang_name}"

class basePost(models.Model):
    title = models.CharField(max_length=50)
    text = models.CharField(max_length=250)
    slug = models.SlugField(null=True)
    createDate = models.DateTimeField(auto_now_add=True)
    language = models.ForeignKey(Language, on_delete=models.CASCADE)
    author = models.ManyToManyField(User)
    pType = postType
    
    class Meta:
        ordering = ['-createDate']
    
    def __str__(self) -> str:
        return f"{self.title} {self.author}"

class requestPost(basePost):
    pType = postType.request
    
    def __init__(self,*args,**kwargs):
        super(basePost, self).__init__(*args,**kwargs)
    

class codePost(basePost):
    ninjaPoints = models.IntegerField(default=0)
    pType = postType.code
    
    def __init__(self,*args,**kwargs):
        super(basePost, self).__init__(*args,**kwargs)
    
    def __str__(self) -> str:
        return super().__str__()
    
    
