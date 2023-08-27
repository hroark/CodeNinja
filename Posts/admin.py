from django.contrib import admin

from .models import Language, requestPost,codePost

admin.site.register(requestPost)
admin.site.register(codePost)
admin.site.register(Language)